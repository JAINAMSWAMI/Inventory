namespace Inventory.Models
{
    public class GetShipmentDetailsModel
    {
        public List<GetShipmentDetailsModel> Items { get; set; } = new();
        public string? Order_No { get; set; }
        public DateOnly Order_Date { get; set; }
        public decimal Order_Total_Cost { get; set; }
        public string? Shipmnt_Status { get; set; }
        public string? Warehouse_Name { get; set; }
        public string? Shipping_Address { get; set; }
        public DateOnly Delivery_Expected_Date { get; set; }
        public string? Shipping_Company { get; set; }
        public string? Shipmnt_Tracking_No { get; set; }
        public string? Customer_Name { get; set; }
        public string? Customer_Email { get; set; }
        public string? Customer_Phone { get; set; }
        public string? State_Name { get; set; }
        public string? City_Name { get; set; }
        public string? Pincode { get; set; }
        public string? Order_Priority { get; set; }
        public string? Order_Notes { get; set; }
        public string? Product_Name { get; set; }
        public int Quantity { get; set; }
    }
}
