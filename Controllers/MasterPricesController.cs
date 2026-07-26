using DataLayer;
using Inventory.Infrastructure;
using Inventory.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace Inventory.Controllers
{
    [Authorize]
    public class MasterPricesController : Controller
    {
        private readonly ILogger<MasterPricesController> _logger;

        public MasterPricesController(ILogger<MasterPricesController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            ViewData["Title"] = "Master Prices";
            return View(LoadPage(new MasterPriceModel()));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Save(MasterPriceModel model)
        {
            var product = FindProduct(model.Electronic_Id);
            if (product == null)
                ModelState.AddModelError(nameof(model.Electronic_Id), "The selected product no longer exists.");
            else
                model.Product_Category = product.Category;

            if (!ModelState.IsValid)
            {
                ViewData["Title"] = "Master Prices";
                ViewData["OpenPriceModal"] = true;
                return View("Index", LoadPage(model));
            }

            var result = new MasterPrice
            {
                Master_Price_Id = model.Master_Price_Id,
                Electronic_Id = model.Electronic_Id,
                Product_Category = model.Product_Category,
                Product_Variant = model.Product_Variant?.Trim(),
                From_Quantity = model.From_Quantity,
                To_Quantity = model.To_Quantity,
                Purchase_Price = model.Purchase_Price,
                Selling_Price = model.Selling_Price
            }.Save();

            if (result > 0)
            {
                TempData["Success"] = model.Master_Price_Id > 0
                    ? "Master price updated."
                    : "Master price added.";
                return RedirectToAction(nameof(Index));
            }

            if (result == -2)
                ModelState.AddModelError(nameof(model.From_Quantity),
                    "This quantity range overlaps an existing price for the same product variant.");
            else
                ModelState.AddModelError(string.Empty,
                    "Could not save the master price. Please try again or contact your administrator.");

            ViewData["Title"] = "Master Prices";
            ViewData["OpenPriceModal"] = true;
            return View("Index", LoadPage(model));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var result = new MasterPrice().Delete(id);
            if (result > 0)
                TempData["Success"] = "Master price deleted.";
            else
                TempData["Error"] = "Could not delete the master price.";

            return RedirectToAction(nameof(Index));
        }

        private MasterPriceModel LoadPage(MasterPriceModel form)
        {
            try
            {
                foreach (DataRow row in new MasterPrice().GetAll().Rows)
                    form.Items.Add(MapPrice(row));
            }
            catch (Exception ex)
            {
                ControllerError.Capture(this, _logger, ex,
                    "Failed to load master prices for {UserId}",
                    User.Identity?.Name ?? "anonymous");
            }

            try
            {
                foreach (DataRow row in new CRUD_Electronic().SelectElectronicData().Rows)
                {
                    var category = Text(row, "Electronic_Sub_Category");
                    var variant = Text(row, "Model_Number");
                    if (string.IsNullOrWhiteSpace(variant))
                        variant = Text(row, "Electronic_Brand");

                    form.Products.Add(new MasterPriceProductOption
                    {
                        Id = Convert.ToInt32(row["Electronic_Id"]),
                        Name = Text(row, "Electronic_Name"),
                        Category = category,
                        Variant = variant
                    });
                }

                form.Categories = form.Products
                    .Select(x => x.Category)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x)
                    .ToList();
            }
            catch (Exception ex)
            {
                ControllerError.CaptureWarning(this, _logger, ex,
                    "Failed to load products for master prices for {UserId}",
                    User.Identity?.Name ?? "anonymous");
            }

            return form;
        }

        private MasterPriceProductOption? FindProduct(int id)
        {
            if (id <= 0) return null;

            try
            {
                var table = new CRUD_Electronic().GetElectronicById(id);
                if (table.Rows.Count == 0) return null;
                var row = table.Rows[0];
                return new MasterPriceProductOption
                {
                    Id = id,
                    Name = Text(row, "Electronic_Name"),
                    Category = Text(row, "Electronic_Sub_Category"),
                    Variant = string.IsNullOrWhiteSpace(Text(row, "Model_Number"))
                        ? Text(row, "Electronic_Brand")
                        : Text(row, "Model_Number")
                };
            }
            catch (Exception ex)
            {
                ControllerError.CaptureWarning(this, _logger, ex,
                    "Failed to validate product {ProductId} for master prices",
                    id);
                return null;
            }
        }

        private static MasterPriceModel MapPrice(DataRow row) => new()
        {
            Master_Price_Id = Convert.ToInt32(row["Master_Price_Id"]),
            Electronic_Id = Convert.ToInt32(row["Electronic_Id"]),
            Product_Category = Text(row, "Product_Category"),
            Product_Name = Text(row, "Product_Name"),
            Product_Variant = Text(row, "Product_Variant"),
            From_Quantity = Convert.ToInt32(row["From_Quantity"]),
            To_Quantity = Convert.ToInt32(row["To_Quantity"]),
            Purchase_Price = Convert.ToDecimal(row["Purchase_Price"]),
            Selling_Price = Convert.ToDecimal(row["Selling_Price"])
        };

        private static string Text(DataRow row, string column) =>
            row.Table.Columns.Contains(column) && row[column] != DBNull.Value
                ? row[column]?.ToString() ?? ""
                : "";
    }
}
