using DataLayer;
using Inventory.Infrastructure;
using Inventory.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace Inventory.Controllers
{
    [Authorize]
    public class SupplierController : Controller
    {
        private readonly ILogger<SupplierController> _logger;

        public SupplierController(ILogger<SupplierController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            ViewData["Title"] = "Suppliers";
            return View(LoadList());
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewData["Title"] = "Add Supplier";
            return View(new SupplierModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(SupplierModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var dl = new Supplier
            {
                Supplier_Name = model.Supplier_Name,
                Contact_Person = model.Contact_Person,
                Email = model.Email,
                Phone = model.Phone,
                Address = model.Address,
                Status = model.Status ?? "Active",
                Created_Date = DateTime.Now
            };

            if (dl.Insert() > 0)
            {
                TempData["Success"] = "Supplier added.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, "Failed to add supplier. Please try again.");
            return View(model);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var dt = new Supplier().GetById(id);
            if (dt.Rows.Count == 0) return NotFound();
            var row = dt.Rows[0];
            ViewData["Title"] = "Edit Supplier";
            return View(Map(row));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(SupplierModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var dl = new Supplier
            {
                Supplier_Id = model.Supplier_Id,
                Supplier_Name = model.Supplier_Name,
                Contact_Person = model.Contact_Person,
                Email = model.Email,
                Phone = model.Phone,
                Address = model.Address,
                Status = model.Status
            };

            if (dl.Update() > 0)
            {
                TempData["Success"] = "Supplier updated.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, "Failed to update supplier. Please try again.");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            if (new Supplier().Delete(id) > 0)
                TempData["Success"] = "Supplier deleted.";
            else
                TempData["Error"] = "Could not delete supplier.";
            return RedirectToAction(nameof(Index));
        }

        private SupplierModel LoadList()
        {
            var model = new SupplierModel();
            try
            {
                foreach (DataRow row in new Supplier().GetAll().Rows)
                    model.Items.Add(Map(row));
            }
            catch (Exception ex)
            {
                ControllerError.Capture(this, _logger, ex,
                    "Failed to load suppliers for {UserId}",
                    User.Identity?.Name ?? "anonymous");
            }
            return model;
        }

        private static SupplierModel Map(DataRow row) => new()
        {
            Supplier_Id = Convert.ToInt32(row["Supplier_Id"]),
            Supplier_Name = row["Supplier_Name"]?.ToString(),
            Contact_Person = row["Contact_Person"]?.ToString(),
            Email = row["Email"]?.ToString(),
            Phone = row["Phone"]?.ToString(),
            Address = row["Address"]?.ToString(),
            Status = row["Status"]?.ToString(),
            Created_Date = row["Created_Date"] != DBNull.Value ? Convert.ToDateTime(row["Created_Date"]) : DateTime.Now
        };
    }
}
