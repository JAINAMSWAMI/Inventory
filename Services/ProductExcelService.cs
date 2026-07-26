using ClosedXML.Excel;
using Inventory.Models;
using System.Globalization;

namespace Inventory.Services
{
    public static class ProductExcelService
    {
        public const int MaximumRows = 1000;

        private static readonly string[] Headers =
        {
            "Category*", "Brand*", "Product Name*", "Sell Price*", "Opening Stock*",
            "Warranty Until*", "Supplier*", "Status*", "SKU", "Barcode", "Model Number",
            "Manufacturer", "UOM", "Unit Cost", "Reorder Level", "Min Stock", "Max Stock",
            "HSN/SAC", "Default Warehouse", "Bin/Rack", "Tax %", "Specifications", "Notes"
        };

        private static readonly string[] RequiredHeaders = Headers.Take(8).ToArray();

        public static byte[] CreateTemplate(string category, string supplier, string warehouse)
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Products");

            for (var column = 1; column <= Headers.Length; column++)
            {
                var cell = sheet.Cell(1, column);
                cell.Value = Headers[column - 1];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0D9488");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            var example = new object[]
            {
                category, "Example Brand", "Example Product", 25000, 10,
                DateTime.Today.AddYears(1), supplier, "Available", "SKU-EXAMPLE-001",
                "890000000001", "MODEL-001", "Example Manufacturer", "PCS", 20000m,
                5, 2, 100, "8471", warehouse, "A-01-R1", 18m,
                "Replace this example row with real product data", "Optional notes"
            };

            for (var column = 1; column <= example.Length; column++)
            {
                var cell = sheet.Cell(2, column);
                switch (example[column - 1])
                {
                    case int intValue: cell.Value = intValue; break;
                    case decimal decimalValue: cell.Value = decimalValue; break;
                    case DateTime dateValue: cell.Value = dateValue; break;
                    default: cell.Value = example[column - 1]?.ToString() ?? ""; break;
                }
            }

            sheet.Cell(2, 6).Style.DateFormat.Format = "yyyy-mm-dd";
            sheet.SheetView.FreezeRows(1);
            sheet.Range(1, 1, 2, Headers.Length).SetAutoFilter();
            sheet.Columns().AdjustToContents();
            foreach (var column in sheet.ColumnsUsed())
                column.Width = Math.Min(column.Width + 2, 32);

            var instructions = workbook.Worksheets.Add("Instructions");
            instructions.Cell("A1").Value = "Fluxx Product Import";
            instructions.Cell("A1").Style.Font.Bold = true;
            instructions.Cell("A1").Style.Font.FontSize = 16;
            instructions.Cell("A3").Value = "How to use";
            instructions.Cell("A3").Style.Font.Bold = true;
            instructions.Cell("A4").Value = "1. Keep the Products sheet and header names unchanged.";
            instructions.Cell("A5").Value = "2. Replace the example row or add products below it.";
            instructions.Cell("A6").Value = "3. Columns ending with * are required.";
            instructions.Cell("A7").Value = "4. Category, Supplier and Default Warehouse must already exist in Fluxx.";
            instructions.Cell("A8").Value = "5. Dates should use yyyy-mm-dd. Prices and quantities must be numbers.";
            instructions.Cell("A9").Value = $"6. Maximum {MaximumRows:N0} products per upload. Only .xlsx files are accepted.";
            instructions.Cell("A10").Value = "7. SKU and Barcode must not duplicate existing products or another row in the file.";
            instructions.Cell("A12").Value = "Allowed Status values";
            instructions.Cell("A12").Style.Font.Bold = true;
            instructions.Cell("A13").Value = "Available, Active, Low Stock, Out of Stock, Discontinued";
            instructions.Cell("A15").Value = "Allowed UOM values";
            instructions.Cell("A15").Style.Font.Bold = true;
            instructions.Cell("A16").Value = "PCS, BOX, SET, KG, LTR";
            instructions.Column("A").Width = 100;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public static ProductImportParseResult Parse(
            Stream stream,
            ISet<string> categories,
            ISet<string> suppliers,
            IReadOnlyDictionary<string, int> warehouses,
            ISet<string> existingSkus,
            ISet<string> existingBarcodes)
        {
            var result = new ProductImportParseResult();

            using var workbook = new XLWorkbook(stream);
            var sheet = workbook.Worksheets.FirstOrDefault(w =>
                string.Equals(w.Name, "Products", StringComparison.OrdinalIgnoreCase))
                ?? workbook.Worksheets.First();

            var lastColumn = sheet.LastColumnUsed()?.ColumnNumber() ?? 0;
            var columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var column = 1; column <= lastColumn; column++)
            {
                var header = sheet.Cell(1, column).GetString().Trim();
                if (!string.IsNullOrWhiteSpace(header))
                    columnMap[header] = column;
            }

            foreach (var required in RequiredHeaders)
            {
                if (!columnMap.ContainsKey(required))
                    result.Errors.Add(new ProductImportError
                    {
                        RowNumber = 1,
                        Message = $"Required column '{required}' is missing. Download a fresh sample template."
                    });
            }

            if (result.Errors.Count > 0)
                return result;

            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
            if (lastRow - 1 > MaximumRows)
            {
                result.Errors.Add(new ProductImportError
                {
                    RowNumber = 0,
                    Message = $"The file contains more than {MaximumRows:N0} product rows. Split it into smaller files."
                });
                return result;
            }

            var fileSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var fileBarcodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
            {
                if (sheet.Row(rowNumber).CellsUsed().All(c => string.IsNullOrWhiteSpace(c.GetString())))
                    continue;

                result.TotalRows++;
                var rowErrors = new List<string>();

                string Text(string header) =>
                    columnMap.TryGetValue(header, out var column)
                        ? sheet.Cell(rowNumber, column).GetString().Trim()
                        : "";

                bool IsBlank(string header) =>
                    !columnMap.TryGetValue(header, out var column) ||
                    string.IsNullOrWhiteSpace(sheet.Cell(rowNumber, column).GetString());

                int Integer(string header, int fallback, int minimum = 0)
                {
                    if (!columnMap.TryGetValue(header, out var column) ||
                        string.IsNullOrWhiteSpace(sheet.Cell(rowNumber, column).GetString()))
                        return fallback;

                    var cell = sheet.Cell(rowNumber, column);
                    if (cell.TryGetValue<int>(out var value) && value >= minimum)
                        return value;

                    rowErrors.Add($"{header} must be a whole number of at least {minimum}.");
                    return fallback;
                }

                decimal DecimalValue(string header, decimal fallback, decimal minimum = 0, decimal maximum = decimal.MaxValue)
                {
                    if (!columnMap.TryGetValue(header, out var column) ||
                        string.IsNullOrWhiteSpace(sheet.Cell(rowNumber, column).GetString()))
                        return fallback;

                    var cell = sheet.Cell(rowNumber, column);
                    if (cell.TryGetValue<decimal>(out var value) && value >= minimum && value <= maximum)
                        return value;

                    rowErrors.Add($"{header} must be between {minimum} and {maximum}.");
                    return fallback;
                }

                DateTime DateValue(string header, DateTime fallback)
                {
                    var cell = sheet.Cell(rowNumber, columnMap[header]);
                    if (cell.TryGetValue<DateTime>(out var value))
                        return value.Date;
                    if (DateTime.TryParse(cell.GetString(), CultureInfo.InvariantCulture,
                        DateTimeStyles.AllowWhiteSpaces, out value))
                        return value.Date;

                    rowErrors.Add($"{header} must be a valid date (yyyy-mm-dd).");
                    return fallback;
                }

                var category = Text("Category*");
                var brand = Text("Brand*");
                var name = Text("Product Name*");
                var supplier = Text("Supplier*");
                var status = Text("Status*");
                var sku = Text("SKU");
                var barcode = Text("Barcode");
                var warehouseName = Text("Default Warehouse");

                if (string.IsNullOrWhiteSpace(category)) rowErrors.Add("Category is required.");
                else if (!categories.Contains(category)) rowErrors.Add($"Category '{category}' does not exist.");
                if (string.IsNullOrWhiteSpace(brand)) rowErrors.Add("Brand is required.");
                if (string.IsNullOrWhiteSpace(name)) rowErrors.Add("Product Name is required.");
                if (IsBlank("Sell Price*")) rowErrors.Add("Sell Price is required.");
                if (IsBlank("Opening Stock*")) rowErrors.Add("Opening Stock is required.");
                if (IsBlank("Warranty Until*")) rowErrors.Add("Warranty Until is required.");
                if (string.IsNullOrWhiteSpace(supplier)) rowErrors.Add("Supplier is required.");
                else if (!suppliers.Contains(supplier)) rowErrors.Add($"Supplier '{supplier}' does not exist.");

                var allowedStatuses = new[] { "Available", "Active", "Low Stock", "Out of Stock", "Discontinued" };
                if (!allowedStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
                    rowErrors.Add("Status must be Available, Active, Low Stock, Out of Stock or Discontinued.");

                if (!string.IsNullOrWhiteSpace(sku) &&
                    (existingSkus.Contains(sku) || !fileSkus.Add(sku)))
                    rowErrors.Add($"SKU '{sku}' is duplicated.");
                if (!string.IsNullOrWhiteSpace(barcode) &&
                    (existingBarcodes.Contains(barcode) || !fileBarcodes.Add(barcode)))
                    rowErrors.Add($"Barcode '{barcode}' is duplicated.");

                int? warehouseId = null;
                if (!string.IsNullOrWhiteSpace(warehouseName))
                {
                    if (warehouses.TryGetValue(warehouseName, out var id))
                        warehouseId = id;
                    else
                        rowErrors.Add($"Warehouse '{warehouseName}' does not exist.");
                }

                var product = new ElectronicModel
                {
                    Electronic_Sub_Category = category,
                    Electronic_Brand = brand,
                    Electronic_Name = name,
                    Electronic_Price = Integer("Sell Price*", 0),
                    Electronic_CRStock = Integer("Opening Stock*", 0),
                    Electronic_Warrenty_Period = DateValue("Warranty Until*", DateTime.Today.AddYears(1)),
                    Electronic_Supplier = supplier,
                    Electronic_Status = status,
                    SKU = sku,
                    Barcode = barcode,
                    Model_Number = Text("Model Number"),
                    Manufacturer = Text("Manufacturer"),
                    Unit_Of_Measure = string.IsNullOrWhiteSpace(Text("UOM")) ? "PCS" : Text("UOM").ToUpperInvariant(),
                    Unit_Cost = DecimalValue("Unit Cost", 0),
                    Reorder_Level = Integer("Reorder Level", 10),
                    Min_Stock = Integer("Min Stock", 5),
                    Max_Stock = Integer("Max Stock", 1000),
                    HSN_Code = Text("HSN/SAC"),
                    Default_Warehouse_Id = warehouseId,
                    Location_Bin = Text("Bin/Rack"),
                    Tax_Percent = DecimalValue("Tax %", 18, 0, 100),
                    Electronic_Specs = Text("Specifications"),
                    Notes = Text("Notes")
                };

                var allowedUoms = new[] { "PCS", "BOX", "SET", "KG", "LTR" };
                if (!allowedUoms.Contains(product.Unit_Of_Measure, StringComparer.OrdinalIgnoreCase))
                    rowErrors.Add("UOM must be PCS, BOX, SET, KG or LTR.");
                if (product.Max_Stock < product.Min_Stock)
                    rowErrors.Add("Max Stock cannot be less than Min Stock.");

                if (rowErrors.Count == 0)
                    result.Products.Add(product);
                else
                    result.Errors.Add(new ProductImportError
                    {
                        RowNumber = rowNumber,
                        Message = string.Join(" ", rowErrors)
                    });
            }

            if (result.TotalRows == 0)
                result.Errors.Add(new ProductImportError { RowNumber = 0, Message = "The Products sheet has no data rows." });

            return result;
        }
    }
}
