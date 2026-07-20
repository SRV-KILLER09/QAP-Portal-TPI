namespace QAP_Portal.MVC.Models
{
    public class CreateQapViewModel
    {
        public string? PoNumber { get; set; }

        public List<LineItemGroupInput> Groups { get; set; } = new();
        public IFormFile? TechnicalSpecificationFile { get; set; }
        public IFormFile? PurchaseOrderCopyFile { get; set; }

        public string? Remarks { get; set; }
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
        public string Remarks { get; set; } = string.Empty; 
    }
}