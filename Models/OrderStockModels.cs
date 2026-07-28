using System.ComponentModel.DataAnnotations;

namespace Inventory.Models
{
    public class CreateOrderModel
    {
        public string? Order_No { get; set; }
        public string? Shipmnt_Tracking_No { get; set; }

        [Required]
        [Display(Name = "Order date")]
        [DataType(DataType.Date)]
        public DateTime Order_Date { get; set; } = DateTime.Today;

        [Required]
        [Display(Name = "Payment type")]
        public string Payment_Type { get; set; } = string.Empty;

        [Required, Range(0.01, double.MaxValue)]
        [Display(Name = "Order total")]
        public decimal Order_Total_Cost { get; set; }

        [Required]
        [Display(Name = "Payment status")]
        public string Payment_Status { get; set; } = "Pending";

        [Required]
        [Display(Name = "Shipping address")]
        public string Shipping_Address { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Expected delivery")]
        [DataType(DataType.Date)]
        public DateTime Delivery_Expected_Date { get; set; } = DateTime.Today.AddDays(5);

        [Required]
        [Display(Name = "Shipment status")]
        public string Shipmnt_Status { get; set; } = "Processing";

        [Required]
        [Display(Name = "Shipping company")]
        public string Shipping_Company { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Warehouse")]
        public string Warehouse_Name { get; set; } = string.Empty;

        [Required, StringLength(150)]
        [Display(Name = "Customer name")]
        public string Customer_Name { get; set; } = string.Empty;

        [EmailAddress]
        [Display(Name = "Customer email")]
        public string? Customer_Email { get; set; }

        [Phone, StringLength(50)]
        [Display(Name = "Customer phone")]
        public string? Customer_Phone { get; set; }

        [Required]
        [Display(Name = "State")]
        public int? State_Id { get; set; }

        [Required]
        [Display(Name = "City")]
        public int? City_Id { get; set; }

        [StringLength(20)]
        public string? Pincode { get; set; }

        [Display(Name = "Priority")]
        public string Order_Priority { get; set; } = "Normal";

        [StringLength(500)]
        [Display(Name = "Order notes")]
        public string? Order_Notes { get; set; }

        [Display(Name = "Product")]
        public int? Electronic_Id { get; set; }

        [Range(0, int.MaxValue)]
        public int Quantity { get; set; }

        public List<OrderLineItemModel> LineItems { get; set; } = new();

        [StringLength(200)]
        [Display(Name = "Billing company")]
        public string? Billing_Company { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Est. dispatch date")]
        public DateTime? Est_Dispatch_Date { get; set; }

        [StringLength(150)]
        [Display(Name = "Contractor")]
        public string? Contractor { get; set; }

        [StringLength(100)]
        [Display(Name = "PO number")]
        public string? PO_Number { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "PO date")]
        public DateTime? PO_Date { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Start date")]
        public DateTime? Start_Date { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Completion date")]
        public DateTime? Completion_Date { get; set; }

        [StringLength(200)]
        [Display(Name = "Project title")]
        public string? Project_Title { get; set; }

        [Display(Name = "Delivery terms")]
        public string? Delivery_Terms { get; set; }

        [Display(Name = "Payment terms")]
        public string? Payment_Terms { get; set; }

        [Display(Name = "Lead source")]
        public string? Lead_Source { get; set; }

        [StringLength(200)]
        [Display(Name = "Other source")]
        public string? Other_Source { get; set; }
    }

    public class OrderLineItemModel
    {
        public int Electronic_Id { get; set; }
        public string? Product_Label { get; set; }
        public string? HSN_Code { get; set; }
        public string Unit_Of_Measure { get; set; } = "Pcs";
        public int Quantity { get; set; } = 1;
        public decimal? Unit_Price { get; set; }

        /// <summary>Percent or Amount</summary>
        public string Discount_Type { get; set; } = "Percent";
        public decimal Discount_Value { get; set; }
        public decimal Discount_Amount { get; set; }
        public decimal Tax_Percent { get; set; }
        public decimal Tax_Amount { get; set; }
        public decimal Line_Total { get; set; }
    }

    public class ProductKitItemModel
    {
        public Guid KitItemId { get; set; }
        public Guid KitId { get; set; }
        public int ProductId { get; set; }
        public string? Brand { get; set; }
        public string? ProductName { get; set; }
        public string? ProductLabel { get; set; }
        public int StockOnHand { get; set; }
        public int SellPrice { get; set; }
        public int DefaultQuantity { get; set; } = 1;
    }

    public class ProductKitAddLineModel
    {
        public int ProductId { get; set; }
        public int DefaultQuantity { get; set; } = 1;
        public bool Selected { get; set; }
    }

    public class ProductKitPageModel
    {
        public Guid? KitId { get; set; }
        public string KitName { get; set; } = "Standard Kit";
        public List<ProductKitAddLineModel> AddLines { get; set; } = new();
        public List<ProductKitItemModel> Items { get; set; } = new();
    }

    public class StockReceiveModel
    {
        public string? Receipt_No { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Receipt date")]
        public DateTime Receipt_Date { get; set; } = DateTime.Today;

        [Display(Name = "Supplier")]
        public int? Supplier_Id { get; set; }

        [Display(Name = "Warehouse")]
        public int? Warehouse_Id { get; set; }

        [Display(Name = "PO / Reference")]
        public string? Reference_No { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        [Display(Name = "Received by")]
        public string? Received_By { get; set; }

        [Required]
        [Display(Name = "Product")]
        public int Electronic_Id { get; set; }

        [Required, Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Range(0, double.MaxValue)]
        [Display(Name = "Unit cost")]
        public decimal? Unit_Cost { get; set; }

        public List<StockReceiveListItem> Items { get; set; } = new();
        public List<StockMovementItem> Movements { get; set; } = new();
    }

    public class StockReceiveListItem
    {
        public int Receipt_Id { get; set; }
        public string? Receipt_No { get; set; }
        public DateTime Receipt_Date { get; set; }
        public string? Supplier_Name { get; set; }
        public string? Warehouse_Name { get; set; }
        public string? Reference_No { get; set; }
        public string? Received_By { get; set; }
        public string? Status { get; set; }
        public int TotalQty { get; set; }
    }

    public class StockMovementItem
    {
        public int Movement_Id { get; set; }
        public string? Electronic_Name { get; set; }
        public string? Electronic_Brand { get; set; }
        public int QuantityDelta { get; set; }
        public string? Movement_Type { get; set; }
        public string? Reason { get; set; }
        public string? Reference_No { get; set; }
        public string? Warehouse_Name { get; set; }
        public DateTime Created_Date { get; set; }
        public int CurrentStock { get; set; }
    }

    public class LookupOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
