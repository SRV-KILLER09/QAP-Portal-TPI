using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QAP.Portal.API.Models
{
    [Table("PO_DOCUMENTS")]
    public class PoDocument
    {
        [Key]
        [Column("PO")]
        [MaxLength(10)]
        public string Po { get; set; } = null!;

        [Column("TECH_SPEC")]
        public byte[]? TechSpec { get; set; }

        [Column("PO_COPY")]
        public byte[]? PoCopy { get; set; }

        [Column("UPDATED_ON")]
        public DateTime? UpdatedOn { get; set; }

        [Column("UPDATED_BY")]
        [MaxLength(50)]
        public string? UpdatedBy { get; set; }
    }
}