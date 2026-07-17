namespace QAP_Portal.MVC.Models
{
    // A single "line item" reference within a group - Line + ItemNo identify
    // the row in MBA_PO_DETAILS; Description is resolved separately (only
    // available once we've also fetched the PO's line items).
    public class LineItemRef
    {
        public int Line { get; set; }
        public int ItemNo { get; set; }
        public string? Description { get; set; }
    }

    // One row in "My QAPs" / "QAP Approvals" - built by joining
    // QapLineGroups + QapGroupItems + GroupActionLogs client-side,
    // since the API has no filtered/joined listing endpoint.
    public class QapGroupSummary
    {
        public int GroupId { get; set; }
        public string? QapNumber { get; set; }
        public QapStatus Status { get; set; }
        public string? PoNumber { get; set; }
        public List<LineItemRef> LineItems { get; set; } = new();

        public string? InitiatedBy { get; set; }
        public DateTime? InitiatedOn { get; set; }

        public string? LastActionBy { get; set; }
        public DateTime? LastActionOn { get; set; }
        public string? LastRemarks { get; set; }
        public string? AssignedAdmin { get; set; }
    }

    // Full detail page model - summary plus resolved documents/PO info/log history.
    public class QapGroupDetail : QapGroupSummary
    {
        public string? PoDescription { get; set; }
        public string? VendorCode { get; set; }
        public DateTime? PoDate { get; set; }

        public bool HasQapDocument { get; set; }
        public bool HasDrawing { get; set; }
        public bool HasTechSpec { get; set; }
        public bool HasPoCopy { get; set; }

        public List<Models.Api.GroupActionLogDto> ActionLogs { get; set; } = new();
    }

    // Search-result view for Step 1 of the wizard.
    public class PoSearchResultViewModel
    {
        public string PoNumber { get; set; } = string.Empty;
        public string? PoDescription { get; set; }
        public string? VendorCode { get; set; }
        public DateTime? PoDate { get; set; }
        public List<LineItemRef> LineItems { get; set; } = new();
        public List<PoLineItemFull> FullLineItems { get; set; } = new();
    }

    public class PoLineItemFull
    {
        public int Line { get; set; }
        public int ItemNo { get; set; }
        public string? Description { get; set; }
        public decimal? QtyOrdered { get; set; }
        public string? Uom { get; set; }
    }

    public class AdminUserViewModel
    {
        public string AdminId { get; set; } = string.Empty;
        public string AdminName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}