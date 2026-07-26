using System.ComponentModel.DataAnnotations;

namespace Inventory.Models
{
    public class InvoiceListItem
    {
        public int Invoice_Id { get; set; }
        public string Invoice_No { get; set; } = string.Empty;
        public string Business_Name { get; set; } = string.Empty;
        public string? Project_Title { get; set; }
        public string? First_Item_Label { get; set; }
        public string? Linked_Order_No { get; set; }
        public string? Business_Segment { get; set; }
        public string? Business_Category { get; set; }
        public string Status { get; set; } = "Draft";
        public string Payment_Status { get; set; } = "Unpaid";
        public decimal Amount { get; set; }
        public decimal Received_Amount { get; set; }
        public string? Assignee { get; set; }
        public DateTime Invoice_Date { get; set; }
        public string? Created_By { get; set; }
        public DateTime Created_At { get; set; }

        public decimal Due_Amount => Math.Max(0, Amount - Received_Amount);
    }

    public class InvoiceLineItemModel
    {
        public int? Electronic_Id { get; set; }
        public string Product_Label { get; set; } = string.Empty;
        [Range(0.0001, double.MaxValue)]
        public decimal Quantity { get; set; } = 1;
        [Range(0, double.MaxValue)]
        public decimal Unit_Price { get; set; }
    }

    public class InvoiceCreateModel
    {
        public string? Invoice_No { get; set; }
        public string Invoice_Type { get; set; } = "Tax";

        [Required, DataType(DataType.Date), Display(Name = "Invoice date")]
        public DateTime Invoice_Date { get; set; } = DateTime.Today;

        [DataType(DataType.Date), Display(Name = "Due date")]
        public DateTime? Due_Date { get; set; }

        public string Status { get; set; } = "Draft";
        public string Payment_Status { get; set; } = "Unpaid";

        [Display(Name = "Business segment")]
        public string? Business_Segment { get; set; } = "Product Sales";

        [Display(Name = "Business category")]
        public string? Business_Category { get; set; } = "B2B";

        [Display(Name = "Business sub category")]
        public string? Business_SubCategory { get; set; }

        public int? Customer_Id { get; set; }

        [Required, StringLength(200), Display(Name = "Business name")]
        public string Business_Name { get; set; } = string.Empty;

        public string? Contact_Title { get; set; } = "Mr.";
        public string? Contact_Name { get; set; }
        public string? Contact_Phone { get; set; }

        [Display(Name = "Billing company")]
        public string? Billing_Company { get; set; }

        public string? PO_Number { get; set; }
        [DataType(DataType.Date)]
        public DateTime? PO_Date { get; set; }
        public string? Project_Title { get; set; }
        public string? Delivery_Terms { get; set; }
        public string? Payment_Terms { get; set; }
        public string? Warehouse_Name { get; set; }
        public string? Linked_Order_No { get; set; }
        public string? Assignee { get; set; }

        public bool Ship_Same_As_Bill { get; set; } = true;
        public string? Ship_Consignee { get; set; }
        public string? Ship_Contact_Name { get; set; }
        public string? Ship_Contact_Phone { get; set; }
        public string? Ship_Address1 { get; set; }
        public string? Ship_Address2 { get; set; }
        public int? Ship_State_Id { get; set; }
        public int? Ship_City_Id { get; set; }
        public string? Ship_Pincode { get; set; }
        public string? Ship_GSTIN { get; set; }
        public string? Remarks { get; set; }

        public List<InvoiceLineItemModel> LineItems { get; set; } = new() { new() };
    }

    public class PaymentReceiptModel
    {
        public string? Receipt_No { get; set; }

        [Required, DataType(DataType.Date)]
        public DateTime Receipt_Date { get; set; } = DateTime.Today;

        [Required, StringLength(200), Display(Name = "Party / customer")]
        public string Party_Name { get; set; } = string.Empty;

        [Display(Name = "Invoice #")]
        public string? Invoice_No { get; set; }

        [Required, Display(Name = "Payment mode")]
        public string Payment_Mode { get; set; } = "Cash";

        [Required, Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        public string? Reference_No { get; set; }
        public string? Remarks { get; set; }
    }

    public class PaymentReceiptListItem
    {
        public int Receipt_Id { get; set; }
        public string Receipt_No { get; set; } = string.Empty;
        public DateTime Receipt_Date { get; set; }
        public string Party_Name { get; set; } = string.Empty;
        public string? Invoice_No { get; set; }
        public string Payment_Mode { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? Created_By { get; set; }
        public DateTime Created_At { get; set; }
    }

    public class ExpenseBookingModel
    {
        public string? Expense_No { get; set; }

        [Required, DataType(DataType.Date)]
        public DateTime Expense_Date { get; set; } = DateTime.Today;

        [Required, StringLength(100)]
        public string Category { get; set; } = string.Empty;

        [Display(Name = "Vendor")]
        public string? Vendor_Name { get; set; }

        public string? Description { get; set; }

        [Required, Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [Display(Name = "Payment mode")]
        public string? Payment_Mode { get; set; } = "Cash";

        public string Status { get; set; } = "Booked";
    }

    public class ExpenseBookingListItem
    {
        public int Expense_Id { get; set; }
        public string Expense_No { get; set; } = string.Empty;
        public DateTime Expense_Date { get; set; }
        public string Category { get; set; } = string.Empty;
        public string? Vendor_Name { get; set; }
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Created_By { get; set; }
        public DateTime Created_At { get; set; }
    }
}
