using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QAP.Portal.API.Models
{
    [Table("QAP_GROUP_ITEMS")]
    public class QapGroupItem
    {
        [Column("PO")]
        [MaxLength(10)]
        public string Po { get; set; } = null!;

        [Column("LINE")]
        public int Line { get; set; }

        [Column("ITEM_NO")]
        public int ItemNo { get; set; }

        [Column("GROUP_ID")]
        public int GroupId { get; set; }

        [Column("UPDATED_ON")]
        public DateTime? UpdatedOn { get; set; }

        [Column("UPDATED_BY")]
        [MaxLength(50)]
        public string? UpdatedBy { get; set; }

        [ForeignKey(nameof(GroupId))]
        public QapLineGroup? QapLineGroup { get; set; }
    }
}