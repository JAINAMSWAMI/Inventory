using DataLayer;
using Inventory.Infrastructure;
using Inventory.Models;
using Inventory.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;

namespace Inventory.Controllers
{
    [Authorize]
    public class InventoryControlController : Controller
    {
        private readonly ILogger<InventoryControlController> _logger;

        public InventoryControlController(ILogger<InventoryControlController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteElectronic(int id)
        {
            var ge = new CRUD_Electronic();
            int result = ge.DeleteElectronic(id);
            if (result > 0)
            {
                TempData["Success"] = "Product deleted.";
                return RedirectToAction(nameof(GetElectronicData));
            }

            TempData["Error"] = "Failed to delete product.";
            return RedirectToAction(nameof(GetElectronicData));
        }

        [HttpGet]
        public IActionResult GetElectronicData(string? search, string? status)
        {
            ViewData["Title"] = "Products";
            var ge = new CRUD_Electronic();
            DataTable dt = string.IsNullOrWhiteSpace(search) && string.IsNullOrWhiteSpace(status)
                ? ge.SelectElectronicData()
                : ge.SearchElectronics(search, status);

            var data = new List<GetElectronicModel>();
            foreach (DataRow row in dt.Rows)
                data.Add(MapRow(row));

            ViewBag.Search = search;
            ViewBag.Status = status;
            return View(new GetElectronicModel { Data = data });
        }

        [HttpGet]
        public IActionResult BulkUpload()
        {
            ViewData["Title"] = "Import Products";
            return View(new ProductImportModel());
        }

        [HttpGet]
        public IActionResult DownloadProductTemplate()
        {
            try
            {
                var category = new Category().GetAll().Rows.Cast<DataRow>()
                    .Select(r => r["Category_Type"]?.ToString())
                    .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "Existing Category";
                var supplier = new Supplier().GetAll().Rows.Cast<DataRow>()
                    .Select(r => r["Supplier_Name"]?.ToString())
                    .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "Existing Supplier";
                var warehouse = new Warehouse().GetAll().Rows.Cast<DataRow>()
                    .Select(r => r["Warehouse_Name"]?.ToString())
                    .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "";

                var bytes = ProductExcelService.CreateTemplate(category, supplier, warehouse);
                return File(bytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Fluxx_Product_Import_Template_{DateTime.Today:yyyyMMdd}.xlsx");
            }
            catch (Exception ex)
            {
                ControllerError.Capture(this, _logger, ex,
                    "Product template generation failed for {UserId}",
                    User.Identity?.Name ?? "anonymous");
                return RedirectToAction(nameof(BulkUpload));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public IActionResult BulkUpload(ProductImportModel model)
        {
            ViewData["Title"] = "Import Products";

            if (model.File == null || model.File.Length == 0)
            {
                ModelState.AddModelError(nameof(model.File), "Select an Excel file to upload.");
                return View(model);
            }

            if (model.File.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError(nameof(model.File), "The Excel file must be 5 MB or smaller.");
                return View(model);
            }

            if (!string.Equals(Path.GetExtension(model.File.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(model.File), "Only .xlsx Excel files are accepted. Download the sample template.");
                return View(model);
            }

            try
            {
                var categories = new HashSet<string>(
                    new Category().GetAll().Rows.Cast<DataRow>()
                        .Select(r => r["Category_Type"]?.ToString() ?? "")
                        .Where(x => !string.IsNullOrWhiteSpace(x)),
                    StringComparer.OrdinalIgnoreCase);

                var suppliers = new HashSet<string>(
                    new Supplier().GetAll().Rows.Cast<DataRow>()
                        .Select(r => r["Supplier_Name"]?.ToString() ?? "")
                        .Where(x => !string.IsNullOrWhiteSpace(x)),
                    StringComparer.OrdinalIgnoreCase);

                var warehouses = new Warehouse().GetAll().Rows.Cast<DataRow>()
                    .Where(r => !string.IsNullOrWhiteSpace(r["Warehouse_Name"]?.ToString()))
                    .GroupBy(r => r["Warehouse_Name"]?.ToString() ?? "", StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => Convert.ToInt32(g.First()["Warehouse_Id"]),
                        StringComparer.OrdinalIgnoreCase);

                var existing = new CRUD_Electronic().SelectElectronicData();
                var existingSkus = new HashSet<string>(
                    existing.Rows.Cast<DataRow>().Select(r => Col(r, "SKU") ?? "")
                        .Where(x => !string.IsNullOrWhiteSpace(x)),
                    StringComparer.OrdinalIgnoreCase);
                var existingBarcodes = new HashSet<string>(
                    existing.Rows.Cast<DataRow>().Select(r => Col(r, "Barcode") ?? "")
                        .Where(x => !string.IsNullOrWhiteSpace(x)),
                    StringComparer.OrdinalIgnoreCase);

                using var stream = model.File.OpenReadStream();
                var parsed = ProductExcelService.Parse(
                    stream, categories, suppliers, warehouses, existingSkus, existingBarcodes);

                model.TotalRows = parsed.TotalRows;
                model.ValidRows = parsed.Products.Count;
                model.Errors = parsed.Errors;
                if (!parsed.IsValid)
                {
                    ModelState.AddModelError(string.Empty,
                        $"Nothing was imported. Correct the {parsed.Errors.Count} validation issue(s) shown below.");
                    return View(model);
                }

                var inserted = CRUD_Electronic.BulkInsert(parsed.Products.Select(MapToData).ToList());
                TempData["Success"] = $"{inserted} product{(inserted == 1 ? "" : "s")} imported from Excel successfully.";
                return RedirectToAction(nameof(GetElectronicData));
            }
            catch (Exception ex)
            {
                ControllerError.AddModelError(this, _logger, ex,
                    "Product Excel import failed for {FileName} by {UserId}",
                    model.File.FileName, User.Identity?.Name ?? "anonymous");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult LowStock()
        {
            ViewData["Title"] = "Low Stock";
            var data = new List<GetElectronicModel>();
            foreach (DataRow row in new CRUD_Electronic().GetLowStock(10).Rows)
                data.Add(MapRow(row));
            return View(new GetElectronicModel { Data = data });
        }

        [HttpGet]
        public IActionResult AdjustStock(int id)
        {
            var dt = new CRUD_Electronic().GetElectronicById(id);
            if (dt.Rows.Count == 0)
            {
                dt = new CRUD_Electronic().SelectElectronicData();
                DataRow? found = null;
                foreach (DataRow row in dt.Rows)
                {
                    if (Convert.ToInt32(row["Electronic_Id"]) == id)
                    {
                        found = row;
                        break;
                    }
                }
                if (found == null) return NotFound();
                ViewData["Title"] = "Adjust Stock";
                return View(new StockAdjustModel
                {
                    Electronic_Id = id,
                    Electronic_Name = found["Electronic_Name"]?.ToString(),
                    CurrentStock = Convert.ToInt32(found["Electronic_CRStock"])
                });
            }

            var r = dt.Rows[0];
            ViewData["Title"] = "Adjust Stock";
            return View(new StockAdjustModel
            {
                Electronic_Id = id,
                Electronic_Name = r["Electronic_Name"]?.ToString(),
                CurrentStock = Convert.ToInt32(r["Electronic_CRStock"])
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AdjustStock(StockAdjustModel model)
        {
            if (model.QuantityDelta == 0)
            {
                ModelState.AddModelError(nameof(model.QuantityDelta), "Enter a non-zero adjustment.");
                return View(model);
            }

            var result = new CRUD_Electronic().AdjustStock(
                model.Electronic_Id,
                model.QuantityDelta,
                model.Reason ?? "Manual adjustment");

            if (result > 0)
            {
                TempData["Success"] = "Stock adjusted.";
                return RedirectToAction(nameof(GetElectronicData));
            }

            TempData["Error"] = "Stock adjustment failed. Ensure AdjustElectronicStock is deployed.";
            return View(model);
        }

        [HttpGet]
        public ActionResult AddElectronic()
        {
            ViewData["Title"] = "Add Product";
            LoadProductLookups();
            return View(new ElectronicModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddElectronic(ElectronicModel electronicModel)
        {
            if (!ModelState.IsValid)
            {
                LoadProductLookups();
                return View(electronicModel);
            }

            try
            {
                var ae = MapToData(electronicModel);
                if (ae.AddNewElectronic() > 0)
                {
                    TempData["Success"] = "Product added.";
                    return RedirectToAction(nameof(GetElectronicData));
                }

                ModelState.AddModelError(string.Empty, "Failed to add product.");
            }
            catch (Exception ex)
            {
                ControllerError.AddModelError(this, _logger, ex,
                    "Add product failed for {UserId}",
                    User.Identity?.Name ?? "anonymous");
            }

            LoadProductLookups();
            return View(electronicModel);
        }

        [HttpGet]
        public IActionResult EditElectronic(int id)
        {
            var dt = new CRUD_Electronic().GetElectronicById(id);
            if (dt.Rows.Count == 0)
            {
                dt = new CRUD_Electronic().SelectElectronicData();
                DataRow? found = null;
                foreach (DataRow row in dt.Rows)
                {
                    if (Convert.ToInt32(row["Electronic_Id"]) == id)
                    {
                        found = row;
                        break;
                    }
                }
                if (found == null)
                {
                    _logger.LogWarning("Electronic item with id {Id} not found.", id);
                    return NotFound();
                }
                ViewData["Title"] = "Edit Product";
                LoadProductLookups();
                return View(MapToModel(found));
            }

            ViewData["Title"] = "Edit Product";
            LoadProductLookups();
            return View(MapToModel(dt.Rows[0]));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditElectronic(ElectronicModel electronicModel)
        {
            if (!ModelState.IsValid)
            {
                LoadProductLookups();
                return View(electronicModel);
            }

            try
            {
                var ge = MapToData(electronicModel);
                if (ge.EditRecord(electronicModel.Electronic_Id) > 0)
                {
                    TempData["Success"] = "Product updated.";
                    return RedirectToAction(nameof(GetElectronicData));
                }

                ModelState.AddModelError(string.Empty, "Failed to update product.");
            }
            catch (Exception ex)
            {
                ControllerError.AddModelError(this, _logger, ex,
                    "Update product {ProductId} failed for {UserId}",
                    electronicModel.Electronic_Id, User.Identity?.Name ?? "anonymous");
            }

            LoadProductLookups();
            return View(electronicModel);
        }

        private void LoadProductLookups()
        {
            try
            {
                var categories = new List<SelectListItem> { new("-- Select category --", "") };
                foreach (DataRow row in new Category().GetAll().Rows)
                {
                    if (row["Category_Status"]?.ToString() == "Active" || string.IsNullOrEmpty(row["Category_Status"]?.ToString()))
                        categories.Add(new(row["Category_Type"]?.ToString() ?? "", row["Category_Type"]?.ToString()));
                }
                ViewBag.Categories = new SelectList(categories, "Value", "Text");

                var suppliers = new List<SelectListItem> { new("-- Select supplier --", "") };
                foreach (DataRow row in new Supplier().GetAll().Rows)
                    suppliers.Add(new(row["Supplier_Name"]?.ToString() ?? "", row["Supplier_Name"]?.ToString()));
                ViewBag.Suppliers = new SelectList(suppliers, "Value", "Text");

                var warehouses = new List<SelectListItem> { new("-- Select warehouse --", "") };
                foreach (DataRow row in new Warehouse().GetAll().Rows)
                    warehouses.Add(new(row["Warehouse_Name"]?.ToString() ?? "", row["Warehouse_Id"]?.ToString()));
                ViewBag.Warehouses = new SelectList(warehouses, "Value", "Text");
            }
            catch (Exception ex)
            {
                ControllerError.CaptureWarning(this, _logger, ex,
                    "Failed to load product lookups for {UserId}",
                    User.Identity?.Name ?? "anonymous");
                ViewBag.Categories = new SelectList(new[] { new SelectListItem("-- Add categories first --", "") }, "Value", "Text");
                ViewBag.Suppliers = new SelectList(new[] { new SelectListItem("-- Add suppliers first --", "") }, "Value", "Text");
                ViewBag.Warehouses = new SelectList(new[] { new SelectListItem("-- Add warehouses first --", "") }, "Value", "Text");
            }
        }

        private static CRUD_Electronic MapToData(ElectronicModel m) => new()
        {
            Electronic_Id = m.Electronic_Id,
            Electronic_Sub_Category = m.Electronic_Sub_Category,
            Electronic_Brand = m.Electronic_Brand,
            Electronic_Name = m.Electronic_Name,
            Electronic_Price = m.Electronic_Price,
            Electronic_CRStock = m.Electronic_CRStock,
            Electronic_Warrenty_Period = m.Electronic_Warrenty_Period,
            Electronic_Specs = m.Electronic_Specs,
            Electronic_Supplier = m.Electronic_Supplier,
            Electronic_Status = m.Electronic_Status,
            SKU = m.SKU,
            Barcode = m.Barcode,
            Model_Number = m.Model_Number,
            Manufacturer = m.Manufacturer,
            Unit_Of_Measure = m.Unit_Of_Measure,
            Unit_Cost = m.Unit_Cost,
            Reorder_Level = m.Reorder_Level,
            Min_Stock = m.Min_Stock,
            Max_Stock = m.Max_Stock,
            HSN_Code = m.HSN_Code,
            Default_Warehouse_Id = m.Default_Warehouse_Id,
            Location_Bin = m.Location_Bin,
            Tax_Percent = m.Tax_Percent,
            Notes = m.Notes
        };

        private static ElectronicModel MapToModel(DataRow row) => new()
        {
            Electronic_Id = Convert.ToInt32(row["Electronic_Id"]),
            Electronic_Sub_Category = row["Electronic_Sub_Category"]?.ToString() ?? "",
            Electronic_Brand = row["Electronic_Brand"]?.ToString() ?? "",
            Electronic_Name = row["Electronic_Name"]?.ToString() ?? "",
            Electronic_Price = Convert.ToInt32(row["Electronic_Price"]),
            Electronic_CRStock = Convert.ToInt32(row["Electronic_CRStock"]),
            Electronic_Warrenty_Period = row["Electronic_Warrenty_Period"] != DBNull.Value
                ? Convert.ToDateTime(row["Electronic_Warrenty_Period"]) : DateTime.Today.AddYears(1),
            Electronic_Specs = row["Electronic_Specs"]?.ToString(),
            Electronic_Supplier = row["Electronic_Supplier"]?.ToString() ?? "",
            Electronic_Status = row["Electronic_Status"]?.ToString() ?? "Available",
            SKU = Col(row, "SKU"),
            Barcode = Col(row, "Barcode"),
            Model_Number = Col(row, "Model_Number"),
            Manufacturer = Col(row, "Manufacturer"),
            Unit_Of_Measure = Col(row, "Unit_Of_Measure") ?? "PCS",
            Unit_Cost = SafeDec(row, "Unit_Cost"),
            Reorder_Level = SafeInt(row, "Reorder_Level", 10),
            Min_Stock = SafeInt(row, "Min_Stock", 5),
            Max_Stock = SafeInt(row, "Max_Stock", 1000),
            HSN_Code = Col(row, "HSN_Code"),
            Default_Warehouse_Id = row.Table.Columns.Contains("Default_Warehouse_Id") && row["Default_Warehouse_Id"] != DBNull.Value
                ? Convert.ToInt32(row["Default_Warehouse_Id"]) : null,
            Location_Bin = Col(row, "Location_Bin"),
            Tax_Percent = SafeDec(row, "Tax_Percent", 18),
            Notes = Col(row, "Notes"),
            Default_Warehouse_Name = Col(row, "Default_Warehouse_Name")
        };

        private static GetElectronicModel MapRow(DataRow row) => new()
        {
            Electronic_Id = Convert.ToInt32(row["Electronic_Id"]),
            Electronic_Sub_Category = row["Electronic_Sub_Category"]?.ToString(),
            Electronic_Brand = row["Electronic_Brand"]?.ToString(),
            Electronic_Name = row["Electronic_Name"]?.ToString(),
            Electronic_Price = Convert.ToInt32(row["Electronic_Price"]),
            Electronic_CRStock = Convert.ToInt32(row["Electronic_CRStock"]),
            Electronic_Warrenty_Period = row["Electronic_Warrenty_Period"] != DBNull.Value
                ? Convert.ToDateTime(row["Electronic_Warrenty_Period"]) : DateTime.MinValue,
            Electronic_Specs = row["Electronic_Specs"]?.ToString(),
            Electronic_Supplier = row["Electronic_Supplier"]?.ToString(),
            Electronic_Status = row["Electronic_Status"]?.ToString(),
            SKU = Col(row, "SKU"),
            Barcode = Col(row, "Barcode"),
            Model_Number = Col(row, "Model_Number"),
            Manufacturer = Col(row, "Manufacturer"),
            Unit_Of_Measure = Col(row, "Unit_Of_Measure"),
            Unit_Cost = SafeDec(row, "Unit_Cost"),
            Reorder_Level = SafeInt(row, "Reorder_Level"),
            Min_Stock = SafeInt(row, "Min_Stock"),
            Max_Stock = SafeInt(row, "Max_Stock"),
            HSN_Code = Col(row, "HSN_Code"),
            Default_Warehouse_Id = row.Table.Columns.Contains("Default_Warehouse_Id") && row["Default_Warehouse_Id"] != DBNull.Value
                ? Convert.ToInt32(row["Default_Warehouse_Id"]) : null,
            Default_Warehouse_Name = Col(row, "Default_Warehouse_Name"),
            Location_Bin = Col(row, "Location_Bin"),
            Tax_Percent = SafeDec(row, "Tax_Percent"),
            Notes = Col(row, "Notes")
        };

        private static string? Col(DataRow row, string name) =>
            row.Table.Columns.Contains(name) ? row[name]?.ToString() : null;

        private static int SafeInt(DataRow row, string col, int fallback = 0) =>
            row.Table.Columns.Contains(col) && row[col] != DBNull.Value ? Convert.ToInt32(row[col]) : fallback;

        private static decimal SafeDec(DataRow row, string col, decimal fallback = 0) =>
            row.Table.Columns.Contains(col) && row[col] != DBNull.Value ? Convert.ToDecimal(row[col]) : fallback;
    }
}
