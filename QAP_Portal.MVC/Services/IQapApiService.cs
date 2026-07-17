using QAP_Portal.MVC.Models;

namespace QAP_Portal.MVC.Services
{
    public interface IQapApiService
    {
        // Purchase Orders
        Task<PoSearchResultViewModel?> SearchPurchaseOrderAsync(string poNumber);


        // QAP Creation
        Task<List<int>> CreateQapAsync(
            CreateQapViewModel model,
            string initiatorEmail);



        // QAP Listing
        Task<List<QapGroupSummary>> GetAllQapGroupsAsync(
            QapStatus? status = null);


        Task<List<QapGroupSummary>> GetQapGroupsForInitiatorAsync(
            string initiatorEmail,
            QapStatus? status = null);


        Task<QapGroupDetail?> GetQapGroupDetailAsync(
            int groupId);



        // Workflow
        Task<bool> SubmitAsync(
            int groupId,
            string actionBy);


        Task<bool> ApproveAsync(
            int groupId,
            string actionBy);


        Task<bool> RejectAsync(
            int groupId,
            string actionBy,
            string remarks);


        Task<bool> ReopenAsync(
            int groupId,
            string actionBy);



        // Documents
        Task<byte[]?> GetQapDocumentBytesAsync(
            int groupId);


        Task<byte[]?> GetDrawingBytesAsync(
            int groupId);


        Task<byte[]?> GetTechSpecBytesAsync(
            string poNumber);


        Task<byte[]?> GetPoCopyBytesAsync(
            string poNumber);



        // ADMIN_USERS validation
        Task<bool> ValidateAdminAsync(string email);
        Task<QAP_Portal.MVC.Models.Api.AdminLoginResult?> LoginAdminAsync(string email, string password);

        // QAP_USERS validation
        Task<QAP_Portal.MVC.Models.Api.QapUserLoginResult?> LoginQapUserAsync(string email, string password);

        // New endpoints for real-time creation and delete support
        Task<bool> DeleteQapGroupAsync(int groupId);
        Task<(bool Success, string ErrorMessage)> CreatePurchaseOrderAsync(CreatePoViewModel model);
        Task<List<AdminUserViewModel>> GetAdminsAsync();
        Task<bool> CreateQapUserAsync(string email, string displayName, string role, string password);
        Task<List<QAP_Portal.MVC.Models.Api.QapUserViewModel>> GetPendingUsersAsync();
        Task<bool> ApproveUserAsync(string email);
        Task<bool> RejectUserAsync(string email);
        Task<(bool Success, string ErrorMessage)> ChangePasswordAsync(string email, string currentPassword, string newPassword);
    }
}