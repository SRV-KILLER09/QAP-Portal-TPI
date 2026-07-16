namespace QAP_Portal.MVC.Models.Api
{
    // ===== Raw shapes returned by / sent to QAP.Portal.API =====
    // Kept 1:1 with the API's entities/DTOs. Property name casing doesn't matter -
    // System.Text.Json binds case-insensitively on both sides.

    public class SapPoMasterDto
    {
        public string PoNumber { get; set; } = string.Empty;
        public string? PoDescription { get; set; }
        public string? VendorCode { get; set; }
        public DateTime? PoDate { get; set; }
        public decimal? PoValue { get; set; }
        public string? PlantCode { get; set; }
        public string? ContactPerson { get; set; }
        public string? Email { get; set; }
        public string? MobileNo { get; set; }
    }

    public class MbaPoDetailsDto
    {
        public string PurchaseOrder { get; set; } = string.Empty;
        public int Item { get; set; }          // == ItemNo used elsewhere
        public int Line { get; set; }
        public string? LineDescription { get; set; }
        public DateTime? CreationDate { get; set; }
        public decimal? QtyOrdered { get; set; }
        public string? Uom { get; set; }
        public decimal? QtyDelivered { get; set; }
        public decimal? UnitPrice { get; set; }
        public string? VendorCode { get; set; }
    }

    // Shape returned by GET api/PurchaseOrders/{po}
    public class PoWithLineItemsDto
    {
        public SapPoMasterDto Header { get; set; } = new();
        public List<MbaPoDetailsDto> LineItems { get; set; } = new();
    }

    public class QapLineGroupDto
    {
        public int GroupId { get; set; }
        public string? QapNumber { get; set; }
        public string? Status { get; set; }   // "D" | "S" | "A" | "R"
        public byte[]? QapDocument { get; set; }
        public byte[]? DrawingDocument { get; set; }
    }

    public class QapGroupItemDto
    {
        public string Po { get; set; } = string.Empty;
        public int Line { get; set; }
        public int ItemNo { get; set; }
        public int GroupId { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string? UpdatedBy { get; set; }
    }

    public class PoDocumentDto
    {
        public string Po { get; set; } = string.Empty;
        public byte[]? TechSpec { get; set; }
        public byte[]? PoCopy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string? UpdatedBy { get; set; }
    }

    public class GroupActionLogDto
    {
        public int SeqNo { get; set; }
        public int GroupId { get; set; }
        public string? Stage { get; set; }     // "I" initiator, "R" review
        public DateTime? ActionOn { get; set; }
        public string? ActionBy { get; set; }
        public string? Remarks { get; set; }
    }

    // ===== Request/response shapes for POST api/QapCreation =====

    public class CreateQapRequestDto
    {
        public string Po { get; set; } = string.Empty;
        public string? InitiatorEmail { get; set; }
        public List<QapGroupRequestDto> Groups { get; set; } = new();
    }

    public class QapGroupRequestDto
    {
        public List<LineItemRequestDto> LineItems { get; set; } = new();
    }

    public class LineItemRequestDto
    {
        public int Line { get; set; }
        public int ItemNo { get; set; }
    }

    public class CreateQapResponseDto
    {
        public string Po { get; set; } = string.Empty;
        public List<CreatedGroupDto> GroupsCreated { get; set; } = new();
    }

    public class CreatedGroupDto
    {
        public int GroupId { get; set; }
        public string? QapNumber { get; set; }
        public string? Status { get; set; }
    }

    // Body for PUT .../approve and .../submit and .../reopen
    public class ActionRequestDto
    {
        public string ActionBy { get; set; } = string.Empty;
    }

    // Body for PUT .../reject
    public class RejectRequestDto
    {
        public string ActionBy { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
    }

    public class AdminLoginResult
    {
        public string AdminId { get; set; } = string.Empty;
        public string AdminName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}