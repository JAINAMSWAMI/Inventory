using DataLayer;
using Inventory.Infrastructure;
using Inventory.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace Inventory.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(ILogger<ReportsController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index(string type = "valuation", DateTime? from = null, DateTime? to = null)
        {
            ViewData["Title"] = "Reports";
            var model = new ReportViewModel
            {
                ReportType = type,
                FromDate = from,
                ToDate = to
            };

            try
            {
                var reports = new Reports();
                DataTable dt = type switch
                {
                    "grn" => reports.GRNReport(from, to),
                    "dcn" => reports.DCNReport(from, to),
                    "movements" => reports.MovementReport(from, to),
                    _ => reports.InventoryValuation()
                };

                foreach (DataColumn col in dt.Columns)
                    model.Columns.Add(col.ColumnName);

                foreach (DataRow row in dt.Rows)
                {
                    var dict = new Dictionary<string, object?>();
                    foreach (DataColumn col in dt.Columns)
                        dict[col.ColumnName] = row[col] == DBNull.Value ? null : row[col];
                    model.Rows.Add(dict);
                }
            }
            catch (Exception ex)
            {
                ControllerError.Capture(this, _logger, ex,
                    "Failed to load report {ReportType} for {UserId}",
                    type, User.Identity?.Name ?? "anonymous");
            }

            return View(model);
        }
    }
}
