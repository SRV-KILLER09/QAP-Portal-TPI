using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QAP.Portal.API.Models
{
    [Table("MBA_PO_DETAILS")]
    public class MbaPoDetails
    {
        [Column("PURCHASE_ORDER")]
        [MaxLength(10)]
        public string PurchaseOrder { get; set; } = null!;

        [Column("ITEM")]
        public int Item { get; set; }

        [Column("LINE")]
        public int Line { get; set; }

        [Column("LINE_DESCRIPTION")]
        public string? LineDescription { get; set; }

        [Column("CREATIONDATE")]
        public DateTime? CreationDate { get; set; }

        [Column("QTY_ORDERED")]
        public decimal? QtyOrdered { get; set; }

        [Column("UOM")]
        public string? Uom { get; set; }

        [Column("QTY_DELIVERED")]
        public decimal? QtyDelivered { get; set; }

        [Column("UNIT_PRICE")]
        public decimal? UnitPrice { get; set; }

        [Column("VENDOR_CODE")]
        public string? VendorCode { get; set; }
    }
}