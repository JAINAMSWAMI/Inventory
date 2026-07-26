using DataLayer;
using Inventory.Infrastructure;
using Inventory.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;

namespace Inventory.Controllers
{
    [Authorize]
    public class StockReceiveController : Controller
    {
        private readonly ILogger<StockReceiveController> _logger;

        public StockReceiveController(ILogger<StockReceiveController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            ViewData["Title"] = "Stock Receive";
            var model = new StockReceiveModel();
            try
            {
                foreach (DataRow row in new StockReceive().GetAll().Rows)
                {
                    model.Items.Add(new StockReceiveListItem
                    {
                        Receipt_Id = Convert.ToInt32(row["Receipt_Id"]),
                        Receipt_No = row["Receipt_No"]?.ToString(),
                        Receipt_Date = row["Receipt_Date"] != DBNull.Value
                            ? Convert.ToDateTime(row["Receipt_Date"]) : DateTime.MinValue,
                        Supplier_Name = row.Table.Columns.Contains("Supplier_Name")
                            ? row["Supplier_Name"]?.ToString() : null,
                        Warehouse_Name = row.Table.Columns.Contains("Warehouse_Name")
                            ? row["Warehouse_Name"]?.ToString() : null,
                        Reference_No = row["Reference_No"]?.ToString(),
                        Received_By = row["Received_By"]?.ToString(),
                        Status = row["Status"]?.ToString(),
                        TotalQty = row.Table.Columns.Contains("TotalQty") && row["TotalQty"] != DBNull.Value
                            ? Convert.ToInt32(row["TotalQty"]) : 0
                    });
                }

                foreach (DataRow row in new StockReceive().GetMovements(30).Rows)
                {
                    model.Movements.Add(new StockMovementItem
                    {
                        Movement_Id = Convert.ToInt32(row["Movement_Id"]),
                        Electronic_Name = row["Electronic_Name"]?.ToString(),
                        Electronic_Brand = row["Electronic_Brand"]?.ToString(),
                        QuantityDelta = Convert.ToInt32(row["QuantityDelta"]),
                        Movement_Type = row.Table.Columns.Contains("Movement_Type")
                            ? row["Movement_Type"]?.ToString() : null,
                        Reason = row["Reason"]?.ToString(),
                        Reference_No = row.Table.Columns.Contains("Reference_No")
                            ? row["Reference_No"]?.ToString() : null,
                        Warehouse_Name = row.Table.Columns.Contains("Warehouse_Name")
                            ? row["Warehouse_Name"]?.ToString() : null,
                        Created_Date = Convert.ToDateTime(row["Created_Date"]),
                        CurrentStock = row.Table.Columns.Contains("Electronic_CRStock")
                            ? Convert.ToInt32(row["Electronic_CRStock"]) : 0
                    });
                }
            }
            catch (Exception ex)
            {
                ControllerError.Capture(this, _logger, ex,
                    "Failed to load stock receipts for {UserId}",
                    User.Identity?.Name ?? "anonymous");
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewData["Title"] = "Receive Stock";
            LoadLookups();
            return View(new StockReceiveModel
            {
                Received_By = User.Identity?.Name
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(StockReceiveModel model)
        {
            if (!ModelState.IsValid)
            {
                LoadLookups();
                return View(model);
            }

            try
            {
                var dl = new StockReceive
                {
                    Receipt_No = "RCV-" + DateTime.Now.ToString("yyyyMMddHHmmss") + "-" + Random.Shared.Next(100, 999),
                    Receipt_Date = model.Receipt_Date,
                    Supplier_Id = model.Supplier_Id,
                    Warehouse_Id = model.Warehouse_Id,
                    Reference_No = model.Reference_No,
                    Notes = model.Notes,
                    Received_By = model.Received_By ?? User.Identity?.Name,
                    Electronic_Id = model.Electronic_Id,
                    Quantity = model.Quantity,
                    Unit_Cost = model.Unit_Cost
                };

                var result = dl.Insert();
                if (result > 0)
                {
                    TempData["Success"] = $"Stock received. Receipt {dl.Receipt_No}.";
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError(string.Empty, "Stock receive failed. Check product and quantity.");
            }
            catch (Exception ex)
            {
                ControllerError.AddModelError(this, _logger, ex,
                    "Stock receive failed for product {ElectronicId} by {UserId}",
                    model.Electronic_Id, User.Identity?.Name ?? "anonymous");
            }

            LoadLookups();
            return View(model);
        }

        [HttpGet]
        public IActionResult Movements()
        {
            ViewData["Title"] = "Stock Movements";
            var model = new StockReceiveModel();
            try
            {
                foreach (DataRow row in new StockReceive().GetMovements(100).Rows)
                {
                    model.Movements.Add(new StockMovementItem
                    {
                        Movement_Id = Convert.ToInt32(row["Movement_Id"]),
                        Electronic_Name = row["Electronic_Name"]?.ToString(),
                        Electronic_Brand = row["Electronic_Brand"]?.ToString(),
                        QuantityDelta = Convert.ToInt32(row["QuantityDelta"]),
                        Movement_Type = row.Table.Columns.Contains("Movement_Type")
                            ? row["Movement_Type"]?.ToString() : null,
                        Reason = row["Reason"]?.ToString(),
                        Reference_No = row.Table.Columns.Contains("Reference_No")
                            ? row["Reference_No"]?.ToString() : null,
                        Warehouse_Name = row.Table.Columns.Contains("Warehouse_Name")
                            ? row["Warehouse_Name"]?.ToString() : null,
                        Created_Date = Convert.ToDateTime(row["Created_Date"]),
                        CurrentStock = row.Table.Columns.Contains("Electronic_CRStock")
                            ? Convert.ToInt32(row["Electronic_CRStock"]) : 0
                    });
                }
            }
            catch (Exception ex)
            {
                ControllerError.Capture(this, _logger, ex,
                    "Failed to load stock movements for {UserId}",
                    User.Identity?.Name ?? "anonymous");
            }

            return View(model);
        }

        private void LoadLookups()
        {
            ViewBag.Products = ToSelectList(new Lookup().GetProducts(), "Electronic_Id", "Electronic_Name");
            ViewBag.Suppliers = ToSelectList(new Supplier().GetAll(), "Supplier_Id", "Supplier_Name");
            ViewBag.Warehouses = ToSelectList(new Warehouse().GetAll(), "Warehouse_Id", "Warehouse_Name");
        }

        private static SelectList ToSelectList(DataTable dt, string valueField, string textField)
        {
            var items = new List<SelectListItem> { new("-- Select --", "") };
            foreach (DataRow row in dt.Rows)
            {
                var text = row[textField]?.ToString() ?? "";
                if (dt.Columns.Contains("Electronic_Brand") && !string.IsNullOrEmpty(row["Electronic_Brand"]?.ToString()))
                    text = $"{row["Electronic_Brand"]} · {text}";
                items.Add(new SelectListItem(text, row[valueField]?.ToString()));
            }
            return new SelectList(items, "Value", "Text");
        }
    }
}
