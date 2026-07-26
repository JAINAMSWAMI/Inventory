using System.ComponentModel.DataAnnotations;

namespace Inventory.Models
{
    public class ElectronicModel
    {
        public int Electronic_Id { get; set; }

        [Required, StringLength(100), Display(Name = "Category")]
        public string Electronic_Sub_Category { get; set; } = string.Empty;

        [Required, StringLength(100), Display(Name = "Brand")]
        public string Electronic_Brand { get; set; } = string.Empty;

        [Required, StringLength(200), Display(Name = "Product name")]
        public string Electronic_Name { get; set; } = string.Empty;

        [Required, Range(0, int.MaxValue), Display(Name = "Sell price")]
        public int Electronic_Price { get; set; }

        [Required, Range(0, int.MaxValue), Display(Name = "Opening stock")]
        public int Electronic_CRStock { get; set; }

        [Required, DataType(DataType.Date), Display(Name = "Warranty until")]
        public DateTime Electronic_Warrenty_Period { get; set; } = DateTime.Today.AddYears(1);

        [StringLength(2000), Display(Name = "Specifications")]
        public string? Electronic_Specs { get; set; }

        [Required, StringLength(200), Display(Name = "Supplier")]
        public string Electronic_Supplier { get; set; } = string.Empty;

        [Required, Display(Name = "Status")]
        public string Electronic_Status { get; set; } = "Available";

        [StringLength(80)]
        public string? SKU { get; set; }

        [StringLength(80)]
        public string? Barcode { get; set; }

        [StringLength(100), Display(Name = "Model number")]
        public string? Model_Number { get; set; }

        [StringLength(150)]
        public string? Manufacturer { get; set; }

        [Display(Name = "UOM")]
        public string Unit_Of_Measure { get; set; } = "PCS";

        [Range(0, double.MaxValue), Display(Name = "Unit cost")]
        public decimal Unit_Cost { get; set; }

        [Range(0, int.MaxValue), Display(Name = "Reorder level")]
        public int Reorder_Level { get; set; } = 10;

        [Range(0, int.MaxValue), Display(Name = "Min stock")]
        public int Min_Stock { get; set; } = 5;

        [Range(0, int.MaxValue), Display(Name = "Max stock")]
        public int Max_Stock { get; set; } = 1000;

        [StringLength(30), Display(Name = "HSN / SAC")]
        public string? HSN_Code { get; set; }

        [Display(Name = "Default warehouse")]
        public int? Default_Warehouse_Id { get; set; }

        [StringLength(50), Display(Name = "Bin / rack")]
        public string? Location_Bin { get; set; }

        [Range(0, 100), Display(Name = "Tax %")]
        public decimal Tax_Percent { get; set; } = 18;

        [StringLength(500)]
        public string? Notes { get; set; }

        public string? Default_Warehouse_Name { get; set; }
    }

    public class GRNLineItemModel
    {
        [Display(Name = "Product")]
        public int Electronic_Id { get; set; }

        /// <summary>Display label for autocomplete after validation errors.</summary>
        public string? Product_Label { get; set; }

        [Range(1, int.MaxValue), Display(Name = "Quantity")]
        public int Quantity { get; set; }

        [Display(Name = "Unit cost")]
        public decimal? Unit_Cost { get; set; }

        [Display(Name = "Batch no.")]
        public string? Batch_No { get; set; }

        [DataType(DataType.Date), Display(Name = "Expiry")]
        public DateTime? Expiry_Date { get; set; }
    }

    public class GRNModel
    {
        public string? GRN_No { get; set; }
        [Required, DataType(DataType.Date), Display(Name = "GRN date")]
        public DateTime GRN_Date { get; set; } = DateTime.Today;

        [Required, Display(Name = "Receive from")]
        public string Party_Type { get; set; } = "Vendor";

        [Display(Name = "Vendor")]
        public int? Supplier_Id { get; set; }

        [Display(Name = "From warehouse")]
        public int? From_Warehouse_Id { get; set; }

        [Display(Name = "To warehouse")]
        public int? To_Warehouse_Id { get; set; }

        [Display(Name = "Vendor invoice no.")]
        public string? Invoice_No { get; set; }

        [DataType(DataType.Date), Display(Name = "Invoice date")]
        public DateTime? Invoice_Date { get; set; }

        [Display(Name = "Vehicle no.")]
        public string? Vehicle_No { get; set; }

        [Display(Name = "Gate entry no.")]
        public string? Gate_Entry_No { get; set; }

        [Display(Name = "Received by")]
        public string? Received_By { get; set; }

        public string? Remarks { get; set; }

        public List<GRNLineItemModel> LineItems { get; set; } = new();

        public List<GRNListItem> Items { get; set; } = new();
    }

    public class GRNListItem
    {
        public int GRN_Id { get; set; }
        public string? GRN_No { get; set; }
        public DateTime GRN_Date { get; set; }
        public string? Party_Type { get; set; }
        public string? Supplier_Name { get; set; }
        public string? From_Warehouse_Name { get; set; }
        public string? To_Warehouse_Name { get; set; }
        public string? Invoice_No { get; set; }
        public string? Status { get; set; }
        public int TotalQty { get; set; }
        public string? Received_By { get; set; }
    }

    public class DCNLineItemModel
    {
        public int Electronic_Id { get; set; }
        public string? Product_Label { get; set; }
        public int Quantity { get; set; }
        public decimal? Unit_Price { get; set; }
    }

    public class DCNModel
    {
        public string? DCN_No { get; set; }
        [Required, DataType(DataType.Date), Display(Name = "DCN date")]
        public DateTime DCN_Date { get; set; } = DateTime.Today;

        [Required, Display(Name = "Dispatch to")]
        public string Party_Type { get; set; } = "Customer";

        [Display(Name = "Customer name")]
        public string? Customer_Name { get; set; }

        [Display(Name = "Customer phone")]
        public string? Customer_Phone { get; set; }

        [EmailAddress, Display(Name = "Customer email")]
        public string? Customer_Email { get; set; }

        [Display(Name = "GSTIN")]
        public string? Customer_GSTIN { get; set; }

        [Display(Name = "Billing company")]
        public string? Billing_Company { get; set; }

        [Display(Name = "PO number")]
        public string? PO_Number { get; set; }

        [Display(Name = "Pincode")]
        public string? Pincode { get; set; }

        [Display(Name = "Delivery address")]
        public string? Customer_Address { get; set; }

        [Display(Name = "From warehouse")]
        public int? From_Warehouse_Id { get; set; }

        [Display(Name = "To warehouse")]
        public int? To_Warehouse_Id { get; set; }

        [Display(Name = "Linked order no.")]
        public string? Order_No { get; set; }

        [Display(Name = "Vehicle no.")]
        public string? Vehicle_No { get; set; }

        public string? Transporter { get; set; }

        [Display(Name = "Dispatched by")]
        public string? Delivered_By { get; set; }

        public string? Remarks { get; set; }

        /// <summary>Legacy single-line fields kept for warehouse transfer.</summary>
        [Display(Name = "Product")]
        public int Electronic_Id { get; set; }

        [Range(0, int.MaxValue)]
        public int Quantity { get; set; }

        [Display(Name = "Unit price")]
        public decimal? Unit_Price { get; set; }

        [Display(Name = "Dispatch unit cost (COGS)")]
        public decimal? DispatchUnitCost { get; set; }

        public List<DCNLineItemModel> LineItems { get; set; } = new();
        public List<DCNListItem> Items { get; set; } = new();
    }

    public class DCNListItem
    {
        public int DCN_Id { get; set; }
        public string? DCN_No { get; set; }
        public DateTime DCN_Date { get; set; }
        public string? Party_Type { get; set; }
        public string? Customer_Name { get; set; }
        public string? From_Warehouse_Name { get; set; }
        public string? To_Warehouse_Name { get; set; }
        public string? Order_No { get; set; }
        public string? Status { get; set; }
        public int TotalQty { get; set; }
    }

    public class ReportViewModel
    {
        public string ReportType { get; set; } = "valuation";
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<Dictionary<string, object?>> Rows { get; set; } = new();
        public List<string> Columns { get; set; } = new();
    }
}
