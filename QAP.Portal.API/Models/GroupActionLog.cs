using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QAP.Portal.API.Models
{
    [Table("GROUP_ACTION_LOG")]
    public class GroupActionLog
    {
        [Key]
        [Column("SEQNO")]
        public int SeqNo { get; set; }

        [Column("GROUP_ID")]
        public int GroupId { get; set; }

        [Column("STAGE")]
        [MaxLength(1)]
        public string? Stage { get; set; }

        [Column("ACTION_ON")]
        public DateTime? ActionOn { get; set; }

        [Column("ACTION_BY")]
        [MaxLength(50)]
        public string? ActionBy { get; set; }

        [Column("REMARKS")]
        [MaxLength(200)]
        public string? Remarks { get; set; }

        [ForeignKey(nameof(GroupId))]
        public QapLineGroup? QapLineGroup { get; set; }
    }
}