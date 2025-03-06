using System.Diagnostics.Contracts;

namespace Inventory.Models
{
    public class CreateOrderModel
    {
       public string Order_No { get; set; }
        public string Shipmnt_Tracking_No { get; set; }
        public DateTime Order_Date { get; set; }
        public string Payment_Type { get; set; }
        public decimal Order_Total_Cost { get; set; }
        public string Payment_Status { get; set; }
        public string Shipping_Address { get; set; }
        public DateTime Delivery_Expected_Date { get; set; }
        public string Shipmnt_Status { get; set; }
        public string Shipping_Company { get; set; }
        public string Warehouse_Name { get; set; }

    }
}
