using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QAP.Portal.API.Models
{
    [Table("QAP_LINE_GROUPS")]
    public class QapLineGroup
    {
        [Key]
        [Column("GROUP_ID")]
        public int GroupId { get; set; }

        [Column("QAP_DOCUMENT")]
        public byte[]? QapDocument { get; set; }

        [Column("DRAWING_DOCUMENT")]
        public byte[]? DrawingDocument { get; set; }

        [Column("QAP_NUMBER")]
        [MaxLength(20)]
        public string? QapNumber { get; set; }

        [Column("STATUS")]
        [MaxLength(1)]
        public string? Status { get; set; }

        [Column("ASSIGNED_ADMIN")]
        [MaxLength(100)]
        public string? AssignedAdmin { get; set; }

        public ICollection<QapGroupItem>? GroupItems { get; set; }
        public ICollection<GroupActionLog>? ActionLogs { get; set; }
    }
}