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
    public class WarehouseController : Controller
    {
        private readonly ILogger<WarehouseController> _logger;

        public WarehouseController(ILogger<WarehouseController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            ViewData["Title"] = "Warehouses";
            return View(LoadList());
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewData["Title"] = "Add Warehouse";
            LoadLookups();
            return View(new WarehouseModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(WarehouseModel model)
        {
            if (!ModelState.IsValid)
            {
                LoadLookups(model.State_Id);
                return View(model);
            }

            var dl = MapToData(model);
            dl.Created_Date = DateTime.Now;

            if (dl.Insert() > 0)
            {
                TempData["Success"] = "Warehouse added.";
                return RedirectToAction(nameof(Index));
            }

            _logger.LogError("Add warehouse failed: {Error}", dl.LastError);
            ModelState.AddModelError(string.Empty, string.IsNullOrWhiteSpace(dl.LastError)
                ? "Failed to add warehouse. Please try again or contact your administrator."
                : $"Failed to add warehouse: {dl.LastError}");
            LoadLookups(model.State_Id);
            return View(model);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var dt = new Warehouse().GetById(id);
            if (dt.Rows.Count == 0) return NotFound();
            var model = Map(dt.Rows[0]);
            ViewData["Title"] = "Edit Warehouse";
            LoadLookups(model.State_Id);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(WarehouseModel model)
        {
            if (!ModelState.IsValid)
            {
                LoadLookups(model.State_Id);
                return View(model);
            }

            var dl = MapToData(model);
            if (dl.Update() > 0)
            {
                TempData["Success"] = "Warehouse updated.";
                return RedirectToAction(nameof(Index));
            }

            _logger.LogError("Update warehouse failed: {Error}", dl.LastError);
            ModelState.AddModelError(string.Empty, string.IsNullOrWhiteSpace(dl.LastError)
                ? "Failed to update warehouse. Please try again or contact your administrator."
                : $"Failed to update warehouse: {dl.LastError}");
            LoadLookups(model.State_Id);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            if (new Warehouse().Delete(id) > 0)
                TempData["Success"] = "Warehouse deleted.";
            else
                TempData["Error"] = "Could not delete warehouse.";
            return RedirectToAction(nameof(Index));
        }

        private void LoadLookups(int? stateId = null)
        {
            ViewBag.States = ToSelect(new Lookup().GetStates(), "State_Id", "State_Name");
            ViewBag.Cities = stateId.HasValue && stateId > 0
                ? ToSelect(new Lookup().GetCitiesByState(stateId.Value), "City_Id", "City_Name")
                : new SelectList(new List<SelectListItem> { new("-- Select state first --", "") }, "Value", "Text");
        }

        private static SelectList ToSelect(DataTable dt, string value, string text)
        {
            var items = new List<SelectListItem> { new("-- Select --", "") };
            foreach (DataRow row in dt.Rows)
                items.Add(new SelectListItem(row[text]?.ToString() ?? "", row[value]?.ToString()));
            return new SelectList(items, "Value", "Text");
        }

        private WarehouseModel LoadList()
        {
            var model = new WarehouseModel();
            try
            {
                foreach (DataRow row in new Warehouse().GetAll().Rows)
                    model.Items.Add(Map(row));
            }
            catch (Exception ex)
            {
                ControllerError.Capture(this, _logger, ex,
                    "Failed to load warehouses for {UserId}",
                    User.Identity?.Name ?? "anonymous");
            }
            return model;
        }

        private static Warehouse MapToData(WarehouseModel model) => new()
        {
            Warehouse_Id = model.Warehouse_Id,
            Warehouse_Name = model.Warehouse_Name,
            Location = model.Location,
            Manager_Name = model.Manager_Name,
            Phone = model.Phone,
            Capacity = model.Capacity,
            Status = model.Status ?? "Active",
            Address_Line = model.Address_Line,
            Pincode = model.Pincode,
            State_Id = model.State_Id,
            City_Id = model.City_Id,
            Email = model.Email,
            Warehouse_Type = model.Warehouse_Type ?? "Distribution",
            Zone = model.Zone,
            Operating_Hours = model.Operating_Hours,
            Is_Primary = model.Is_Primary
        };

        private static WarehouseModel Map(DataRow row) => new()
        {
            Warehouse_Id = Convert.ToInt32(row["Warehouse_Id"]),
            Warehouse_Name = row["Warehouse_Name"]?.ToString(),
            Location = row["Location"]?.ToString(),
            Manager_Name = row["Manager_Name"]?.ToString(),
            Phone = row["Phone"]?.ToString(),
            Capacity = Convert.ToInt32(row["Capacity"]),
            Status = row["Status"]?.ToString(),
            Created_Date = row["Created_Date"] != DBNull.Value ? Convert.ToDateTime(row["Created_Date"]) : DateTime.Now,
            Address_Line = Col(row, "Address_Line"),
            Pincode = Col(row, "Pincode"),
            State_Id = row.Table.Columns.Contains("State_Id") && row["State_Id"] != DBNull.Value
                ? Convert.ToInt32(row["State_Id"]) : null,
            City_Id = row.Table.Columns.Contains("City_Id") && row["City_Id"] != DBNull.Value
                ? Convert.ToInt32(row["City_Id"]) : null,
            State_Name = Col(row, "State_Name"),
            City_Name = Col(row, "City_Name"),
            Email = Col(row, "Email"),
            Warehouse_Type = Col(row, "Warehouse_Type") ?? "Distribution",
            Zone = Col(row, "Zone"),
            Operating_Hours = Col(row, "Operating_Hours"),
            Is_Primary = row.Table.Columns.Contains("Is_Primary") && row["Is_Primary"] != DBNull.Value
                && Convert.ToBoolean(row["Is_Primary"])
        };

        private static string? Col(DataRow row, string name) =>
            row.Table.Columns.Contains(name) ? row[name]?.ToString() : null;
    }
}
