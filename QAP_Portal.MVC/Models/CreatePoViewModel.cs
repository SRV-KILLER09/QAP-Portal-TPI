using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QAP_Portal.MVC.Models
{
    public class CreatePoViewModel
    {
        [Required(ErrorMessage = "PO Number is required.")]
        [MaxLength(10, ErrorMessage = "PO Number must be 10 characters or less.")]
        [Display(Name = "PO Number")]
        public string PoNumber { get; set; } = null!;

        [Display(Name = "PO Description")]
        public string? PoDescription { get; set; }

        [Required(ErrorMessage = "Vendor Code is required.")]
        [Display(Name = "Vendor Code")]
        public string VendorCode { get; set; } = null!;

        [Required(ErrorMessage = "PO Date is required.")]
        [Display(Name = "PO Date")]
        public DateTime? PoDate { get; set; } = DateTime.Today;

        [Display(Name = "PO Value")]
        public decimal? PoValue { get; set; }

        [Display(Name = "Plant Code")]
        public string? PlantCode { get; set; }

        [Display(Name = "Contact Person")]
        public string? ContactPerson { get; set; }

        [EmailAddress(ErrorMessage = "Invalid Email Address.")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Display(Name = "Mobile No")]
        public string? MobileNo { get; set; }

        public List<CreatePoLineItemInput> LineItems { get; set; } = new();
    }

    public class CreatePoLineItemInput
    {
        [Required(ErrorMessage = "Item number is required.")]
        public int Item { get; set; }

        [Required(ErrorMessage = "Line number is required.")]
        public int Line { get; set; }

        [Required(ErrorMessage = "Line Description is required.")]
        [Display(Name = "Line Description")]
        public string LineDescription { get; set; } = null!;

        [Required(ErrorMessage = "Qty Ordered is required.")]
        [Display(Name = "Qty Ordered")]
        public decimal QtyOrdered { get; set; }

        [Required(ErrorMessage = "UOM is required.")]
        [Display(Name = "UOM")]
        public string Uom { get; set; } = "NOS";

        [Required(ErrorMessage = "Unit Price is required.")]
        [Display(Name = "Unit Price")]
        public decimal UnitPrice { get; set; }
    }
}
