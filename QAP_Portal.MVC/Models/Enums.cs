namespace QAP_Portal.MVC.Models
{
    // Mirrors the single-character status codes stored on QAP_LINE_GROUPS.STATUS
    public enum QapStatus
    {
        Draft,      // "D"
        Submitted,  // "S"
        Approved,   // "A"
        Rejected    // "R"
    }

    public static class QapStatusMapper
    {
        public static QapStatus FromCode(string? code) => code switch
        {
            "D" => QapStatus.Draft,
            "S" => QapStatus.Submitted,
            "A" => QapStatus.Approved,
            "R" => QapStatus.Rejected,
            _ => QapStatus.Draft
        };

        public static string ToCode(QapStatus status) => status switch
        {
            QapStatus.Draft => "D",
            QapStatus.Submitted => "S",
            QapStatus.Approved => "A",
            QapStatus.Rejected => "R",
            _ => "D"
        };
    }

    public enum UserRole
    {
        Initiator,
        Admin
    }
}