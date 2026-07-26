namespace Inventory.Models
{
    public class GetElectronicModel
    {
        public List<GetElectronicModel> Data { get; set; } = new();
        public int Electronic_Id { get; set; }
        public string? Electronic_Sub_Category { get; set; }
        public string? Electronic_Brand { get; set; }
        public string? Electronic_Name { get; set; }
        public int Electronic_Price { get; set; }
        public int Electronic_CRStock { get; set; }
        public DateTime Electronic_Warrenty_Period { get; set; }
        public string? Electronic_Specs { get; set; }
        public string? Electronic_Supplier { get; set; }
        public string? Electronic_Status { get; set; }
        public string? SKU { get; set; }
        public string? Barcode { get; set; }
        public string? Model_Number { get; set; }
        public string? Manufacturer { get; set; }
        public string? Unit_Of_Measure { get; set; }
        public decimal Unit_Cost { get; set; }
        public int Reorder_Level { get; set; }
        public int Min_Stock { get; set; }
        public int Max_Stock { get; set; }
        public string? HSN_Code { get; set; }
        public int? Default_Warehouse_Id { get; set; }
        public string? Default_Warehouse_Name { get; set; }
        public string? Location_Bin { get; set; }
        public decimal Tax_Percent { get; set; }
        public string? Notes { get; set; }
    }
}
