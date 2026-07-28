using System.ComponentModel.DataAnnotations;

namespace Inventory.Models
{
    public class DashboardViewModel
    {
        public int TotalProducts { get; set; }
        public int TotalStock { get; set; }
        public int LowStockCount { get; set; }
        public decimal InventoryValue { get; set; }
        public int OpenShipments { get; set; }
        public int TotalOrders { get; set; }
        public decimal OrderRevenue { get; set; }
        public int SupplierCount { get; set; }
        public int WarehouseCount { get; set; }
        public int GRNCount { get; set; }
        public int DCNCount { get; set; }
        public int GRNQty { get; set; }
        public int DCNQty { get; set; }

        public List<RecentShipmentItem> RecentShipments { get; set; } = new();
        public List<CategoryStockItem> StockByCategory { get; set; } = new();
        public List<GetElectronicModel> LowStockItems { get; set; } = new();
        public int PendingApprovals { get; set; }
        public List<PendingApprovalSummary> PendingApprovalItems { get; set; } = new();
    }

    public class PendingApprovalSummary
    {
        public int Id { get; set; }
        public string? FormTypeName { get; set; }
        public string? RequestedByName { get; set; }
        public DateTime RequestedAt { get; set; }
        public int RecordId { get; set; }
    }

    public class RecentShipmentItem
    {
        public string? Order_No { get; set; }
        public DateTime Order_Date { get; set; }
        public decimal Order_Total_Cost { get; set; }
        public string? Shipmnt_Status { get; set; }
        public string? Warehouse_Name { get; set; }
        public string? Shipmnt_Tracking_No { get; set; }
    }

    public class CategoryStockItem
    {
        public string? CategoryName { get; set; }
        public int ProductCount { get; set; }
        public int TotalStock { get; set; }
    }

    public class CategoryModel
    {
        public int Category_Id { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Category name")]
        public string? Category_Type { get; set; }

        [Required]
        public string? Category_Status { get; set; } = "Active";
        public DateTime Category_Added_Date { get; set; } = DateTime.Now;

        [StringLength(500)]
        public string? Category_Description { get; set; }
        public List<CategoryModel> Items { get; set; } = new();
    }

    public class SupplierModel
    {
        public int Supplier_Id { get; set; }

        [Required, StringLength(200)]
        [Display(Name = "Supplier name")]
        public string? Supplier_Name { get; set; }

        [StringLength(150)]
        public string? Contact_Person { get; set; }

        [EmailAddress, StringLength(200)]
        public string? Email { get; set; }

        [Phone, StringLength(50)]
        public string? Phone { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [Required]
        public string? Status { get; set; } = "Active";
        public DateTime Created_Date { get; set; } = DateTime.Now;
        public List<SupplierModel> Items { get; set; } = new();
    }

    public class WarehouseModel
    {
        public int Warehouse_Id { get; set; }

        [Required, StringLength(200)]
        [Display(Name = "Warehouse name")]
        public string? Warehouse_Name { get; set; }

        [StringLength(300)]
        public string? Location { get; set; }

        [StringLength(150)]
        public string? Manager_Name { get; set; }

        [Phone, StringLength(50)]
        public string? Phone { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Capacity cannot be negative.")]
        public int Capacity { get; set; }

        [Required]
        public string? Status { get; set; } = "Active";
        public DateTime Created_Date { get; set; } = DateTime.Now;

        [StringLength(300)]
        [Display(Name = "Address line")]
        public string? Address_Line { get; set; }

        [StringLength(20)]
        public string? Pincode { get; set; }

        [Display(Name = "State")]
        public int? State_Id { get; set; }

        [Display(Name = "City")]
        public int? City_Id { get; set; }

        public string? State_Name { get; set; }
        public string? City_Name { get; set; }

        [EmailAddress, StringLength(200)]
        public string? Email { get; set; }

        [Display(Name = "Warehouse type")]
        public string? Warehouse_Type { get; set; } = "Distribution";

        [StringLength(100)]
        public string? Zone { get; set; }

        [Display(Name = "Operating hours"), StringLength(100)]
        public string? Operating_Hours { get; set; }

        [Display(Name = "Primary warehouse")]
        public bool Is_Primary { get; set; }

        public List<WarehouseModel> Items { get; set; } = new();
    }

    public class StockAdjustModel
    {
        public int Electronic_Id { get; set; }
        public string? Electronic_Name { get; set; }
        public int CurrentStock { get; set; }
        public int QuantityDelta { get; set; }
        public string? Reason { get; set; }
    }
}
