namespace QAP.Portal.API.Models.Dtos
{
    public class CreateQapRequest
    {
        public string Po { get; set; } = null!;
        public string? InitiatorEmail { get; set; }
        public List<QapGroupRequest> Groups { get; set; } = new();
    }

    public class QapGroupRequest
    {
        public List<LineItemRequest> LineItems { get; set; } = new();
    }

    public class LineItemRequest
    {
        public int Line { get; set; }
        public int ItemNo { get; set; }
    }
}