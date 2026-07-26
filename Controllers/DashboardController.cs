using DataLayer;
using Inventory.Infrastructure;
using Inventory.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace Inventory.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(ILogger<DashboardController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var model = new DashboardViewModel();

            try
            {
                var dash = new Dashboard();
                var metrics = dash.GetMetrics();
                if (metrics.Rows.Count > 0)
                {
                    var row = metrics.Rows[0];
                    model.TotalProducts = SafeInt(row, "TotalProducts");
                    model.TotalStock = SafeInt(row, "TotalStock");
                    model.LowStockCount = SafeInt(row, "LowStockCount");
                    model.InventoryValue = SafeDecimal(row, "InventoryValue");
                    model.OpenShipments = SafeInt(row, "OpenShipments");
                    model.TotalOrders = SafeInt(row, "TotalOrders");
                    model.OrderRevenue = SafeDecimal(row, "OrderRevenue");
                    model.SupplierCount = SafeInt(row, "SupplierCount");
                    model.WarehouseCount = SafeInt(row, "WarehouseCount");
                    model.GRNCount = SafeInt(row, "GRNCount");
                    model.DCNCount = SafeInt(row, "DCNCount");
                    model.GRNQty = SafeInt(row, "GRNQty");
                    model.DCNQty = SafeInt(row, "DCNQty");
                }

                foreach (DataRow row in dash.GetRecentShipments(6).Rows)
                {
                    model.RecentShipments.Add(new RecentShipmentItem
                    {
                        Order_No = row["Order_No"]?.ToString(),
                        Order_Date = row["Order_Date"] != DBNull.Value ? Convert.ToDateTime(row["Order_Date"]) : DateTime.MinValue,
                        Order_Total_Cost = SafeDecimal(row, "Order_Total_Cost"),
                        Shipmnt_Status = row["Shipmnt_Status"]?.ToString(),
                        Warehouse_Name = row["Warehouse_Name"]?.ToString(),
                        Shipmnt_Tracking_No = row["Shipmnt_Tracking_No"]?.ToString()
                    });
                }

                foreach (DataRow row in dash.GetStockByCategory().Rows)
                {
                    model.StockByCategory.Add(new CategoryStockItem
                    {
                        CategoryName = row["CategoryName"]?.ToString(),
                        ProductCount = SafeInt(row, "ProductCount"),
                        TotalStock = SafeInt(row, "TotalStock")
                    });
                }

                var electronics = new CRUD_Electronic();
                foreach (DataRow row in electronics.GetLowStock(10).Rows)
                {
                    model.LowStockItems.Add(MapElectronic(row));
                }
            }
            catch (Exception ex)
            {
                ControllerError.Capture(this, _logger, ex,
                    "Failed to load dashboard metrics for {UserId}",
                    User.Identity?.Name ?? "anonymous");
            }

            ViewData["Title"] = "Dashboard";
            return View(model);
        }

        private static GetElectronicModel MapElectronic(DataRow row) => new()
        {
            Electronic_Id = Convert.ToInt32(row["Electronic_Id"]),
            Electronic_Sub_Category = row["Electronic_Sub_Category"]?.ToString(),
            Electronic_Brand = row["Electronic_Brand"]?.ToString(),
            Electronic_Name = row["Electronic_Name"]?.ToString(),
            Electronic_Price = Convert.ToInt32(row["Electronic_Price"]),
            Electronic_CRStock = Convert.ToInt32(row["Electronic_CRStock"]),
            Electronic_Supplier = row["Electronic_Supplier"]?.ToString(),
            Electronic_Status = row["Electronic_Status"]?.ToString()
        };

        private static int SafeInt(DataRow row, string col) =>
            row.Table.Columns.Contains(col) && row[col] != DBNull.Value ? Convert.ToInt32(row[col]) : 0;

        private static decimal SafeDecimal(DataRow row, string col) =>
            row.Table.Columns.Contains(col) && row[col] != DBNull.Value ? Convert.ToDecimal(row[col]) : 0m;
    }
}
