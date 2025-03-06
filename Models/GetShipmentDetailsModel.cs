namespace Inventory.Models
{
    public class GetShipmentDetailsModel
    {
        internal List<GetShipmentDetailsModel> Items;
       
        public string Order_No { get; set; }
       public DateOnly Order_Date { get; set; }
        public int Order_Total_Cost { get; set; }
        public string Shipmnt_Status { get; set; }
        public string Warehouse_Name { get; set; }
        public string Shipping_Address { get; set; }
        public DateOnly Delivery_Expected_Date { get; set; }
        public string Shipping_Company { get; set; }
        public string Shipmnt_Tracking_No { get; set; }

    }
}
