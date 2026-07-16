using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QAP.Portal.API.Models
{
    [Table("SAP_PO_MASTER")]
    public class SapPoMaster
    {
        [Key]
        [Column("PO_NUMBER")]
        [MaxLength(10)]
        public string PoNumber { get; set; } = null!;

        [Column("PO_DESCRIPTION")]
        public string? PoDescription { get; set; }

        [Column("VENDOR_CODE")]
        public string? VendorCode { get; set; }

        [Column("PO_DATE")]
        public DateTime? PoDate { get; set; }

        [Column("PO_VALUE")]
        public decimal? PoValue { get; set; }

        [Column("PLANT_CODE")]
        public string? PlantCode { get; set; }

        [Column("CONTACT_PERSON")]
        public string? ContactPerson { get; set; }

        [Column("EMAIL")]
        public string? Email { get; set; }

        [Column("MOBILENO")]
        public string? MobileNo { get; set; }
    }
}