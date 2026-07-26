using DataLayer;
using Inventory.Infrastructure;
using Inventory.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace Inventory.Controllers
{
    [Authorize]
    public class CategoryController : Controller
    {
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(ILogger<CategoryController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            ViewData["Title"] = "Categories";
            return View(LoadList());
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewData["Title"] = "Add Category";
            return View(new CategoryModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CategoryModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var dl = new Category
            {
                Category_Type = model.Category_Type,
                Category_Status = model.Category_Status ?? "Active",
                Category_Added_Date = DateTime.Now,
                Category_Description = model.Category_Description
            };

            if (dl.Insert() > 0)
            {
                TempData["Success"] = "Category created.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, "Failed to create category. Please try again.");
            return View(model);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var dt = new Category().GetById(id);
            if (dt.Rows.Count == 0) return NotFound();

            var row = dt.Rows[0];
            var model = new CategoryModel
            {
                Category_Id = Convert.ToInt32(row["Category_Id"]),
                Category_Type = row["Category_Type"]?.ToString(),
                Category_Status = row["Category_Status"]?.ToString(),
                Category_Description = row.Table.Columns.Contains("Category_Description")
                    ? row["Category_Description"]?.ToString() : null,
                Category_Added_Date = row["Category_Added_Date"] != DBNull.Value
                    ? Convert.ToDateTime(row["Category_Added_Date"]) : DateTime.Now
            };
            ViewData["Title"] = "Edit Category";
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(CategoryModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var dl = new Category
            {
                Category_Id = model.Category_Id,
                Category_Type = model.Category_Type,
                Category_Status = model.Category_Status,
                Category_Description = model.Category_Description
            };

            if (dl.Update() > 0)
            {
                TempData["Success"] = "Category updated.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, "Failed to update category. Please try again.");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            if (new Category().Delete(id) > 0)
                TempData["Success"] = "Category deleted.";
            else
                TempData["Error"] = "Could not delete category.";
            return RedirectToAction(nameof(Index));
        }

        private CategoryModel LoadList()
        {
            var model = new CategoryModel();
            try
            {
                foreach (DataRow row in new Category().GetAll().Rows)
                {
                    model.Items.Add(new CategoryModel
                    {
                        Category_Id = Convert.ToInt32(row["Category_Id"]),
                        Category_Type = row["Category_Type"]?.ToString(),
                        Category_Status = row["Category_Status"]?.ToString(),
                        Category_Description = row.Table.Columns.Contains("Category_Description")
                            ? row["Category_Description"]?.ToString() : null,
                        Category_Added_Date = row["Category_Added_Date"] != DBNull.Value
                            ? Convert.ToDateTime(row["Category_Added_Date"]) : DateTime.Now
                    });
                }
            }
            catch (Exception ex)
            {
                ControllerError.Capture(this, _logger, ex,
                    "Failed to load categories for {UserId}",
                    User.Identity?.Name ?? "anonymous");
            }
            return model;
        }
    }
}
