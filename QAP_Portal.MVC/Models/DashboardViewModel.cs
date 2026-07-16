namespace QAP_Portal.MVC.Models
{
    public class DashboardViewModel
    {
        public string Role { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public int Total { get; set; }
        public int DraftCount { get; set; }
        public int SubmittedCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }

        public List<QapGroupSummary> RecentQaps { get; set; } = new();
    }
}