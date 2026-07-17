namespace QAP_Portal.MVC.Models
{
    // Backing model for the "New Quality Assurance Plan" wizard.
    // NOTE: the real API's CreateQap call (POST api/QapCreation) has no
    // remarks field and auto-submits (Draft -> Submitted) in the same call -
    // there is no "Save Draft" path today. Remarks is kept in the UI only
    // as a note to the reviewer; it is not sent anywhere by this model.
    public class CreateQapViewModel
    {
        // Step 1 - Select PO
        public string? PoNumber { get; set; }

        // Step 2 - Line Items Under QAP, grouped
        public List<LineItemGroupInput> Groups { get; set; } = new();

        // Step 3 - PO level documents (uploaded via separate calls after creation)
        public IFormFile? TechnicalSpecificationFile { get; set; }
        public IFormFile? PurchaseOrderCopyFile { get; set; }

        public string? Remarks { get; set; } // UI-only, not persisted by the current API
        public string? AssignedAdmin { get; set; }
    }

    public class LineItemGroupInput
    {
        public List<LineRef> LineItems { get; set; } = new();
        public IFormFile? QapDocumentFile { get; set; }
        public IFormFile? DrawingFile { get; set; }
    }

    public class LineRef
    {
        public int Line { get; set; }
        public int ItemNo { get; set; }
    }

    public class RejectQapViewModel
    {
        public int GroupId { get; set; }
        public string? QapNumber { get; set; }
        public string Remarks { get; set; } = string.Empty; // mandatory - API rejects otherwise
    }
}