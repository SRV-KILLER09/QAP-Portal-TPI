using System.Net.Http.Json;
using System.Text.Json;
using QAP_Portal.MVC.Models;
using QAP_Portal.MVC.Models.Api;

namespace QAP_Portal.MVC.Services
{
    // Talks to the real QAP.Portal.API exactly as it exists today:
    // - QapCreation auto-submits (Draft -> Submitted) in one call, no draft support
    // - Documents are uploaded via separate multipart endpoints, AFTER creation
    // - Listing endpoints return unfiltered "get all" - aggregation/filtering happens here
    // - Documents come back as raw byte[] (no filename/content-type stored)
    public class QapApiService : IQapApiService
    {
        private readonly HttpClient _http;
        private readonly ILogger<QapApiService> _logger;
        private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

        public QapApiService(HttpClient http, ILogger<QapApiService> logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task<PoSearchResultViewModel?> SearchPurchaseOrderAsync(string poNumber)
        {
            try
            {
                var response = await _http.GetAsync($"PurchaseOrders/{Uri.EscapeDataString(poNumber)}");
                if (!response.IsSuccessStatusCode) return null;

                var dto = await response.Content.ReadFromJsonAsync<PoWithLineItemsDto>(JsonOpts);
                if (dto is null) return null;

                return new PoSearchResultViewModel
                {
                    PoNumber = dto.Header.PoNumber,
                    PoDescription = dto.Header.PoDescription,
                    VendorCode = dto.Header.VendorCode,
                    PoDate = dto.Header.PoDate,
                    FullLineItems = dto.LineItems.Select(li => new PoLineItemFull
                    {
                        Line = li.Line,
                        ItemNo = li.Item,
                        Description = li.LineDescription,
                        QtyOrdered = li.QtyOrdered,
                        Uom = li.Uom
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching PO {PoNumber}", poNumber);
                return null;
            }
        }

        public async Task<List<int>> CreateQapAsync(CreateQapViewModel model, string initiatorEmail)
        {
            var createdGroupIds = new List<int>();

            // 1) POST api/QapCreation - creates & auto-submits one QapLineGroup per UI group
            var request = new CreateQapRequestDto
            {
                Po = model.PoNumber ?? string.Empty,
                InitiatorEmail = initiatorEmail,
                AssignedAdmin = model.AssignedAdmin,
                Groups = model.Groups.Select(g => new QapGroupRequestDto
                {
                    LineItems = g.LineItems.Select(li => new LineItemRequestDto { Line = li.Line, ItemNo = li.ItemNo }).ToList()
                }).ToList()
            };

            var createResponse = await _http.PostAsJsonAsync("QapCreation", request, JsonOpts);
            if (!createResponse.IsSuccessStatusCode)
            {
                var body = await createResponse.Content.ReadAsStringAsync();
                _logger.LogWarning("QapCreation failed: {Status} {Body}", createResponse.StatusCode, body);
                return createdGroupIds;
            }

            var created = await createResponse.Content.ReadFromJsonAsync<CreateQapResponseDto>(JsonOpts);
            if (created is null || created.GroupsCreated.Count == 0) return createdGroupIds;

            createdGroupIds = created.GroupsCreated.Select(g => g.GroupId).ToList();

            // 2) Upload per-group QAP document / drawing (matched to the request by array order)
            for (int i = 0; i < created.GroupsCreated.Count && i < model.Groups.Count; i++)
            {
                var groupId = created.GroupsCreated[i].GroupId;
                var uiGroup = model.Groups[i];

                if (uiGroup.QapDocumentFile is { Length: > 0 } qapDoc)
                    await UploadFileAsync($"QapLineGroups/{groupId}/upload-qap-document", qapDoc);

                if (uiGroup.DrawingFile is { Length: > 0 } drawing)
                    await UploadFileAsync($"QapLineGroups/{groupId}/upload-drawing", drawing);
            }

            // 3) Upload PO-level Technical Specification / PO Copy
            if (model.TechnicalSpecificationFile is { Length: > 0 } techSpec)
                await UploadFileAsync($"PoDocuments/{model.PoNumber}/upload-tech-spec", techSpec);

            if (model.PurchaseOrderCopyFile is { Length: > 0 } poCopy)
                await UploadFileAsync($"PoDocuments/{model.PoNumber}/upload-po-copy", poCopy);

            return createdGroupIds;
        }

        private async Task<bool> UploadFileAsync(string relativeUrl, IFormFile file)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                using var stream = file.OpenReadStream();
                using var streamContent = new StreamContent(stream);
                content.Add(streamContent, "file", file.FileName); // API's IFormFile parameter is named "file"

                var response = await _http.PostAsync(relativeUrl, content);
                if (!response.IsSuccessStatusCode)
                    _logger.LogWarning("Upload to {Url} failed: {Status}", relativeUrl, response.StatusCode);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file to {Url}", relativeUrl);
                return false;
            }
        }

        public async Task<List<QapGroupSummary>> GetAllQapGroupsAsync(QapStatus? status = null)
        {
            var summaries = await BuildSummariesAsync();
            return status.HasValue
                ? summaries.Where(s => s.Status == status.Value).ToList()
                : summaries;
        }

        public async Task<List<QapGroupSummary>> GetQapGroupsForInitiatorAsync(string initiatorEmail, QapStatus? status = null)
        {
            var summaries = await BuildSummariesAsync();
            var mine = summaries.Where(s =>
                string.Equals(s.InitiatedByEmail, initiatorEmail, StringComparison.OrdinalIgnoreCase)).ToList();
            return status.HasValue ? mine.Where(s => s.Status == status.Value).ToList() : mine;
        }

        // Joins QapLineGroups + QapGroupItems + GroupActionLog client-side since
        // the API exposes no filtered/aggregated listing endpoint.
        private async Task<List<QapGroupSummary>> BuildSummariesAsync()
        {
            var groups = await GetJsonListAsync<QapLineGroupDto>("QapLineGroups");
            var items = await GetJsonListAsync<QapGroupItemDto>("QapGroupItems");
            var logs = await GetJsonListAsync<GroupActionLogDto>("GroupActionLog");
            var users = await GetJsonListAsync<QAP_Portal.MVC.Models.Api.QapUserViewModel>("user");
            var admins = await GetJsonListAsync<AdminUserViewModel>("admin");

            var userNames = users.ToDictionary(u => u.Email.ToLower(), u => u.DisplayName);
            var adminNames = admins.ToDictionary(a => a.Email.ToLower(), a => a.AdminName);

            var result = new List<QapGroupSummary>();
            foreach (var g in groups)
            {
                var groupItems = items.Where(i => i.GroupId == g.GroupId).ToList();
                var groupLogs = logs.Where(l => l.GroupId == g.GroupId).OrderBy(l => l.ActionOn).ToList();
                var initiatedLog = groupLogs.FirstOrDefault(l => l.Stage == "I");
                var lastLog = groupLogs.LastOrDefault();

                string? resolvedLastActionBy = lastLog?.ActionBy;
                if (!string.IsNullOrEmpty(resolvedLastActionBy))
                {
                    var clean = resolvedLastActionBy.Trim().ToLower();
                    if (adminNames.TryGetValue(clean, out var adminName)) resolvedLastActionBy = adminName;
                    else if (userNames.TryGetValue(clean, out var userName)) resolvedLastActionBy = userName;
                }

                string? resolvedInitiatedBy = initiatedLog?.ActionBy;
                if (!string.IsNullOrEmpty(resolvedInitiatedBy))
                {
                    var clean = resolvedInitiatedBy.Trim().ToLower();
                    if (userNames.TryGetValue(clean, out var userName)) resolvedInitiatedBy = userName;
                    else if (adminNames.TryGetValue(clean, out var adminName)) resolvedInitiatedBy = adminName;
                }

                result.Add(new QapGroupSummary
                {
                    GroupId = g.GroupId,
                    QapNumber = g.QapNumber,
                    Status = QapStatusMapper.FromCode(g.Status),
                    PoNumber = groupItems.FirstOrDefault()?.Po,
                    LineItems = groupItems.Select(i => new LineItemRef { Line = i.Line, ItemNo = i.ItemNo }).ToList(),
                    InitiatedBy = resolvedInitiatedBy,
                    InitiatedByEmail = initiatedLog?.ActionBy,
                    InitiatedOn = initiatedLog?.ActionOn,
                    LastActionBy = resolvedLastActionBy,
                    LastActionOn = lastLog?.ActionOn,
                    LastRemarks = lastLog?.Remarks,
                    AssignedAdmin = g.AssignedAdmin
                });
            }

            return result.OrderByDescending(s => s.InitiatedOn).ToList();
        }

        public async Task<QapGroupDetail?> GetQapGroupDetailAsync(int groupId)
        {
            try
            {
                var groupResponse = await _http.GetAsync($"QapLineGroups/{groupId}");
                if (!groupResponse.IsSuccessStatusCode) return null;
                var group = await groupResponse.Content.ReadFromJsonAsync<QapLineGroupDto>(JsonOpts);
                if (group is null) return null;

                var items = await GetJsonListAsync<QapGroupItemDto>("QapGroupItems");
                var groupItems = items.Where(i => i.GroupId == groupId).ToList();
                var poNumber = groupItems.FirstOrDefault()?.Po;

                var logs = await GetJsonListAsync<GroupActionLogDto>("GroupActionLog");
                var groupLogs = logs.Where(l => l.GroupId == groupId).OrderBy(l => l.ActionOn).ToList();
                var initiatedLog = groupLogs.FirstOrDefault(l => l.Stage == "I");
                var lastLog = groupLogs.LastOrDefault();

                var users = await GetJsonListAsync<QAP_Portal.MVC.Models.Api.QapUserViewModel>("user");
                var admins = await GetJsonListAsync<AdminUserViewModel>("admin");
                var userNames = users.ToDictionary(u => u.Email.ToLower(), u => u.DisplayName);
                var adminNames = admins.ToDictionary(a => a.Email.ToLower(), a => a.AdminName);

                foreach (var log in groupLogs)
                {
                    if (!string.IsNullOrEmpty(log.ActionBy))
                    {
                        var clean = log.ActionBy.Trim().ToLower();
                        if (adminNames.TryGetValue(clean, out var adminName)) log.ActionBy = adminName;
                        else if (userNames.TryGetValue(clean, out var userName)) log.ActionBy = userName;
                    }
                }

                string? resolvedLastActionBy = lastLog?.ActionBy;
                if (!string.IsNullOrEmpty(resolvedLastActionBy))
                {
                    var clean = resolvedLastActionBy.Trim().ToLower();
                    if (adminNames.TryGetValue(clean, out var adminName)) resolvedLastActionBy = adminName;
                    else if (userNames.TryGetValue(clean, out var userName)) resolvedLastActionBy = userName;
                }

                string? resolvedInitiatedBy = initiatedLog?.ActionBy;
                if (!string.IsNullOrEmpty(resolvedInitiatedBy))
                {
                    var clean = resolvedInitiatedBy.Trim().ToLower();
                    if (userNames.TryGetValue(clean, out var userName)) resolvedInitiatedBy = userName;
                    else if (adminNames.TryGetValue(clean, out var adminName)) resolvedInitiatedBy = adminName;
                }

                var detail = new QapGroupDetail
                {
                    GroupId = group.GroupId,
                    QapNumber = group.QapNumber,
                    Status = QapStatusMapper.FromCode(group.Status),
                    PoNumber = poNumber,
                    LineItems = groupItems.Select(i => new LineItemRef { Line = i.Line, ItemNo = i.ItemNo }).ToList(),
                    InitiatedBy = resolvedInitiatedBy,
                    InitiatedOn = initiatedLog?.ActionOn,
                    LastActionBy = resolvedLastActionBy,
                    LastActionOn = lastLog?.ActionOn,
                    LastRemarks = lastLog?.Remarks,
                    HasQapDocument = group.QapDocument is { Length: > 0 },
                    HasDrawing = group.DrawingDocument is { Length: > 0 },
                    AssignedAdmin = group.AssignedAdmin,
                    ActionLogs = groupLogs
                };

                // Resolve PO header + line item descriptions, and PO-level document presence
                if (!string.IsNullOrEmpty(poNumber))
                {
                    var po = await SearchPurchaseOrderAsync(poNumber);
                    if (po is not null)
                    {
                        detail.PoDescription = po.PoDescription;
                        detail.VendorCode = po.VendorCode;
                        detail.PoDate = po.PoDate;

                        foreach (var li in detail.LineItems)
                        {
                            var match = po.FullLineItems.FirstOrDefault(f => f.Line == li.Line && f.ItemNo == li.ItemNo);
                            li.Description = match?.Description;
                        }
                    }

                    var poDocResponse = await _http.GetAsync($"PoDocuments/{Uri.EscapeDataString(poNumber)}");
                    if (poDocResponse.IsSuccessStatusCode)
                    {
                        var poDoc = await poDocResponse.Content.ReadFromJsonAsync<PoDocumentDto>(JsonOpts);
                        detail.HasTechSpec = poDoc?.TechSpec is { Length: > 0 };
                        detail.HasPoCopy = poDoc?.PoCopy is { Length: > 0 };
                    }
                }

                return detail;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building QAP group detail for {GroupId}", groupId);
                return null;
            }
        }

        public Task<bool> SubmitAsync(int groupId, string actionBy) =>
            PutActionAsync($"QapLineGroups/{groupId}/submit", new ActionRequestDto { ActionBy = actionBy });

        public Task<bool> ApproveAsync(int groupId, string actionBy) =>
            PutActionAsync($"QapLineGroups/{groupId}/approve", new ActionRequestDto { ActionBy = actionBy });

        public Task<bool> ReopenAsync(int groupId, string actionBy) =>
            PutActionAsync($"QapLineGroups/{groupId}/reopen", new ActionRequestDto { ActionBy = actionBy });

        public async Task<bool> RejectAsync(int groupId, string actionBy, string remarks)
        {
            try
            {
                var response = await _http.PutAsJsonAsync($"QapLineGroups/{groupId}/reject",
                    new RejectRequestDto { ActionBy = actionBy, Remarks = remarks }, JsonOpts);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting group {GroupId}", groupId);
                return false;
            }
        }

        private async Task<bool> PutActionAsync(string relativeUrl, ActionRequestDto body)
        {
            try
            {
                var response = await _http.PutAsJsonAsync(relativeUrl, body, JsonOpts);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing PUT {Url}", relativeUrl);
                return false;
            }
        }

        public async Task<byte[]?> GetQapDocumentBytesAsync(int groupId)
        {
            var response = await _http.GetAsync($"QapLineGroups/{groupId}");
            if (!response.IsSuccessStatusCode) return null;
            var dto = await response.Content.ReadFromJsonAsync<QapLineGroupDto>(JsonOpts);
            return dto?.QapDocument;
        }

        public async Task<byte[]?> GetDrawingBytesAsync(int groupId)
        {
            var response = await _http.GetAsync($"QapLineGroups/{groupId}");
            if (!response.IsSuccessStatusCode) return null;
            var dto = await response.Content.ReadFromJsonAsync<QapLineGroupDto>(JsonOpts);
            return dto?.DrawingDocument;
        }

        public async Task<byte[]?> GetTechSpecBytesAsync(string poNumber)
{
    var response = await _http.GetAsync(
        $"PoDocuments/{Uri.EscapeDataString(poNumber)}");

    if (!response.IsSuccessStatusCode)
        return null;

    var dto = await response.Content.ReadFromJsonAsync<PoDocumentDto>(JsonOpts);

    return dto?.TechSpec;
}


public async Task<byte[]?> GetPoCopyBytesAsync(string poNumber)
{
    var response = await _http.GetAsync(
        $"PoDocuments/{Uri.EscapeDataString(poNumber)}");

    if (!response.IsSuccessStatusCode)
        return null;

    var dto = await response.Content.ReadFromJsonAsync<PoDocumentDto>(JsonOpts);

    return dto?.PoCopy;
}


        public async Task<bool> ValidateAdminAsync(string email)
        {
            try
            {
                var response = await _http.GetAsync(
                    $"Admin/validate?email={Uri.EscapeDataString(email)}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Admin validation failed for {Email}. StatusCode: {StatusCode}",
                        email,
                        response.StatusCode);

                    return false;
                }

                return await response.Content.ReadFromJsonAsync<bool>();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error validating admin {Email}",
                    email);

                return false;
            }
        }


        public async Task<QAP_Portal.MVC.Models.Api.AdminLoginResult?> LoginAdminAsync(string email, string password)
        {
            try
            {
                var requestBody = new { Email = email, Password = password };
                var response = await _http.PostAsJsonAsync("admin/login", requestBody, JsonOpts);

                if (!response.IsSuccessStatusCode)
                {
                    var errorMsg = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Admin login failed for {Email}. Status: {Status}, Error: {Error}", email, response.StatusCode, errorMsg);
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<QAP_Portal.MVC.Models.Api.AdminLoginResult>(JsonOpts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing admin login for {Email}", email);
                return null;
            }
        }

        public async Task<QAP_Portal.MVC.Models.Api.QapUserLoginResult?> LoginQapUserAsync(string email, string password)
        {
            try
            {
                var requestBody = new { Email = email, Password = password };
                var response = await _http.PostAsJsonAsync("user/login", requestBody, JsonOpts);

                if (!response.IsSuccessStatusCode)
                {
                    var errorMsg = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("User login failed for {Email}. Status: {Status}, Error: {Error}", email, response.StatusCode, errorMsg);
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<QAP_Portal.MVC.Models.Api.QapUserLoginResult>(JsonOpts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing user login for {Email}", email);
                return null;
            }
        }

        public async Task<bool> DeleteQapGroupAsync(int groupId)
        {
            try
            {
                var response = await _http.DeleteAsync($"QapLineGroups/{groupId}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting QAP group {GroupId}", groupId);
                return false;
            }
        }

        public async Task<(bool Success, string ErrorMessage)> CreatePurchaseOrderAsync(CreatePoViewModel model)
        {
            try
            {
                var dto = new
                {
                    PoNumber = model.PoNumber,
                    PoDescription = model.PoDescription,
                    VendorCode = model.VendorCode,
                    PoDate = model.PoDate,
                    PoValue = model.PoValue,
                    PlantCode = model.PlantCode,
                    ContactPerson = model.ContactPerson,
                    Email = model.Email,
                    MobileNo = model.MobileNo,
                    LineItems = model.LineItems.Select(x => new
                    {
                        Item = x.Item,
                        Line = x.Line,
                        LineDescription = x.LineDescription,
                        QtyOrdered = x.QtyOrdered,
                        Uom = x.Uom,
                        UnitPrice = x.UnitPrice
                    }).ToList()
                };

                var response = await _http.PostAsJsonAsync("PurchaseOrders", dto, JsonOpts);
                if (response.IsSuccessStatusCode)
                {
                    return (true, string.Empty);
                }

                // Try to parse detailed error message
                string detailsMsg;
                try
                {
                    var errorPayload = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(JsonOpts);
                    detailsMsg = errorPayload != null && errorPayload.TryGetValue("error", out var msg) 
                        ? msg 
                        : $"API returned {(int)response.StatusCode} {response.ReasonPhrase}";
                }
                catch
                {
                    detailsMsg = await response.Content.ReadAsStringAsync();
                    if (string.IsNullOrWhiteSpace(detailsMsg))
                        detailsMsg = $"API returned {(int)response.StatusCode} {response.ReasonPhrase}";
                }

                return (false, detailsMsg);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Purchase Order {PoNumber}", model.PoNumber);
                return (false, ex.Message);
            }
        }
        public async Task<List<AdminUserViewModel>> GetAdminsAsync()
        {
            return await GetJsonListAsync<AdminUserViewModel>("admin");
        }

        public async Task<bool> CreateQapUserAsync(string email, string displayName, string role, string password)
        {
            try
            {
                var body = new { Email = email, DisplayName = displayName, Role = role, Password = password };
                var response = await _http.PostAsJsonAsync("user/create", body, JsonOpts);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user {Email}", email);
                return false;
            }
        }

        public async Task<List<QAP_Portal.MVC.Models.Api.QapUserViewModel>> GetPendingUsersAsync()
        {
            return await GetJsonListAsync<QAP_Portal.MVC.Models.Api.QapUserViewModel>("user/pending");
        }

        public async Task<bool> ApproveUserAsync(string email)
        {
            try
            {
                var body = new { Email = email };
                var response = await _http.PostAsJsonAsync("user/approve", body, JsonOpts);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving user {Email}", email);
                return false;
            }
        }

        public async Task<bool> RejectUserAsync(string email)
        {
            try
            {
                var body = new { Email = email };
                var response = await _http.PostAsJsonAsync("user/reject", body, JsonOpts);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting user {Email}", email);
                return false;
            }
        }

        public async Task<(bool Success, string ErrorMessage)> ChangePasswordAsync(string email, string currentPassword, string newPassword)
        {
            try
            {
                var body = new { Email = email, CurrentPassword = currentPassword, NewPassword = newPassword };
                var response = await _http.PostAsJsonAsync("user/change-password", body, JsonOpts);
                if (response.IsSuccessStatusCode)
                {
                    return (true, string.Empty);
                }

                var errorObj = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(JsonOpts);
                var errorMsg = errorObj != null && errorObj.TryGetValue("error", out var err) ? err : "Failed to change password.";
                return (false, errorMsg);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user {Email}", email);
                return (false, "An unexpected error occurred.");
            }
        }

private async Task<List<T>> GetJsonListAsync<T>(string relativeUrl)
{
    try
    {
        var result = await _http.GetFromJsonAsync<List<T>>(relativeUrl, JsonOpts);

        return result ?? new List<T>();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error fetching {Url}", relativeUrl);

        return new List<T>();
    }
}

    }
}