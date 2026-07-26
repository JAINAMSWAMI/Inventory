using DataLayer;
using Inventory.Infrastructure;
using Inventory.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace Inventory.Controllers
{
    [Authorize]
    public class BomController : Controller
    {
        private readonly ILogger<BomController> _logger;

        public BomController(ILogger<BomController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            ViewData["Title"] = "Product Kit";
            return View(LoadPage());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(ProductKitPageModel model)
        {
            model.AddLines ??= new List<ProductKitAddLineModel>();
            var selected = model.AddLines
                .Where(l => l.Selected && l.ProductId > 0)
                .Select(l => new
                {
                    ProductId = l.ProductId,
                    DefaultQuantity = l.DefaultQuantity > 0 ? l.DefaultQuantity : 1
                })
                .ToList();

            if (selected.Count == 0)
            {
                TempData["Error"] = "Select at least one product to add.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(selected);
                var result = new ProductKitStore().UpsertItems(json, model.KitId);
                TempData["Success"] = result > 0
                    ? $"{selected.Count} product(s) saved to the kit."
                    : "No products were saved.";
            }
            catch (Exception ex)
            {
                ControllerError.Capture(this, _logger, ex,
                    "Product kit add failed for {UserId}",
                    User.Identity?.Name ?? "anonymous");
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(Guid id)
        {
            try
            {
                new ProductKitStore().DeleteItem(id);
                TempData["Success"] = "Product removed from kit.";
            }
            catch (Exception ex)
            {
                ControllerError.Capture(this, _logger, ex,
                    "Product kit delete failed for kit {KitId} by {UserId}",
                    id, User.Identity?.Name ?? "anonymous");
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Products()
        {
            try
            {
                var dt = new ProductKitStore().GetItems();
                var items = new List<object>();
                foreach (DataRow r in dt.Rows)
                {
                    items.Add(new
                    {
                        electronicId = Convert.ToInt32(r["ProductId"]),
                        quantity = Convert.ToInt32(r["DefaultQuantity"]),
                        unitPrice = Convert.ToDecimal(r["SellPrice"]),
                        label = r["ProductLabel"]?.ToString(),
                        stock = Convert.ToInt32(r["StockOnHand"])
                    });
                }
                return Json(new { ok = true, items });
            }
            catch (Exception ex)
            {
                return Json(ControllerError.JsonError(this, _logger, ex,
                    "Product kit Products JSON failed for {UserId}",
                    User.Identity?.Name ?? "anonymous"));
            }
        }

        private ProductKitPageModel LoadPage()
        {
            var model = new ProductKitPageModel();
            try
            {
                var kits = new ProductKitStore().GetKits();
                if (kits.Rows.Count > 0)
                {
                    model.KitId = (Guid)kits.Rows[0]["KitId"];
                    model.KitName = kits.Rows[0]["KitName"]?.ToString() ?? "Standard Kit";
                }

                foreach (DataRow r in new ProductKitStore().GetItems(model.KitId).Rows)
                {
                    model.Items.Add(new ProductKitItemModel
                    {
                        KitItemId = (Guid)r["KitItemId"],
                        KitId = (Guid)r["KitId"],
                        ProductId = Convert.ToInt32(r["ProductId"]),
                        Brand = r["Brand"]?.ToString(),
                        ProductName = r["ProductName"]?.ToString(),
                        ProductLabel = r["ProductLabel"]?.ToString(),
                        StockOnHand = Convert.ToInt32(r["StockOnHand"]),
                        SellPrice = Convert.ToInt32(r["SellPrice"]),
                        DefaultQuantity = Convert.ToInt32(r["DefaultQuantity"])
                    });
                }
            }
            catch (Exception ex)
            {
                ControllerError.Capture(this, _logger, ex,
                    "Product kit load failed for {UserId}",
                    User.Identity?.Name ?? "anonymous");
            }

            var existing = model.Items.Select(i => i.ProductId).ToHashSet();
            var products = new Lookup().GetProducts();
            foreach (DataRow row in products.Rows)
            {
                var id = Convert.ToInt32(row["Electronic_Id"]);
                if (existing.Contains(id)) continue;
                model.AddLines.Add(new ProductKitAddLineModel
                {
                    ProductId = id,
                    DefaultQuantity = 1,
                    Selected = false
                });
            }
            ViewBag.ProductOptions = products;
            return model;
        }
    }
}
