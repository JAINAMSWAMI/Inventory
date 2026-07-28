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
    public class DistributionController : Controller
    {
        private readonly ILogger<DistributionController> _logger;

        public DistributionController(ILogger<DistributionController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult ViewShipment()
        {
            ViewData["Title"] = "Shipments";
            var data = new List<GetShipmentDetailsModel>();

            try
            {
                foreach (DataRow row in new GetShipmentDetails().SelectShippingDetails().Rows)
                {
                    data.Add(MapShipment(row));
                }
            }
            catch (Exception ex)
            {
                ControllerError.Capture(this, _logger, ex,
                    "Failed to load shipments for {UserId}",
                    User.Identity?.Name ?? "anonymous");
            }

            return View(new GetShipmentDetailsModel { Items = data });
        }

        [HttpGet]
        public IActionResult Tracking(string? trackingNo)
        {
            ViewData["Title"] = "Track Shipment";
            ViewBag.TrackingNo = trackingNo;
            return View();
        }

        [HttpGet]
        public JsonResult GetOrderProgress(string trackingNo)
        {
            try
            {
                var parameters = new[]
                {
                    new System.Data.SqlClient.SqlParameter("@Shipmnt_Tracking_No", trackingNo)
                };
                var dt = new Operations().ExecuteProcedureSelect("GetOrderProgressByTrackingNo", parameters);
                if (dt.Rows.Count == 0)
                    return Json(new { found = false, currentStep = 0 });

                var status = dt.Rows[0]["Shipmnt_Status"]?.ToString()?.Trim() ?? "";
                int currentStep = status switch
                {
                    "Processing" => 1,
                    "Shipped" => 2,
                    "Out for Delivery" => 3,
                    "Delivered" => 4,
                    _ => 1
                };

                return Json(new
                {
                    found = true,
                    currentStep,
                    status,
                    orderNo = dt.Rows[0]["Order_No"]?.ToString(),
                    customer = dt.Columns.Contains("Customer_Name") ? dt.Rows[0]["Customer_Name"]?.ToString() : null,
                    product = dt.Columns.Contains("Product_Name") ? dt.Rows[0]["Product_Name"]?.ToString() : null,
                    trackingNo
                });
            }
            catch (Exception ex)
            {
                return Json(ControllerError.JsonError(this, _logger, ex,
                    "Error fetching order progress for {TrackingNo}",
                    trackingNo));
            }
        }

        public IActionResult DistributionCenter()
        {
            ViewData["Title"] = "Network Map";
            return View();
        }

        [HttpGet]
        public IActionResult CreateOrder()
        {
            ViewData["Title"] = "Create Order";
            LoadOrderLookups();
            return View(new CreateOrderModel
            {
                LineItems = { new OrderLineItemModel { Quantity = 1 } }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateOrder(CreateOrderModel model)
        {
            model.LineItems ??= new List<OrderLineItemModel>();
            model.LineItems = model.LineItems
                .Where(l => l.Electronic_Id > 0 || l.Quantity > 0)
                .ToList();

            ModelState.Remove(nameof(model.Electronic_Id));
            ModelState.Remove(nameof(model.Quantity));

            if (model.LineItems.Count == 0)
                ModelState.AddModelError(string.Empty, "Add at least one product line (or click Load Product Kit).");

            for (var i = 0; i < model.LineItems.Count; i++)
            {
                if (model.LineItems[i].Electronic_Id <= 0)
                    ModelState.AddModelError($"LineItems[{i}].Electronic_Id", "Select a product.");
                if (model.LineItems[i].Quantity <= 0)
                    ModelState.AddModelError($"LineItems[{i}].Quantity", "Quantity must be at least 1.");
            }

            if (model.Order_Total_Cost <= 0 && model.LineItems.Any())
            {
                model.Order_Total_Cost = model.LineItems.Sum(l =>
                    l.Line_Total > 0 ? l.Line_Total : (l.Unit_Price ?? 0) * l.Quantity);
                ModelState.Remove(nameof(model.Order_Total_Cost));
            }

            if (!ModelState.IsValid)
            {
                if (model.LineItems.Count == 0)
                    model.LineItems.Add(new OrderLineItemModel { Quantity = 1 });
                LoadOrderLookups(model.State_Id);
                return View(model);
            }

            model.Order_No = new NumberFormatStore().NextNumber("Order", "ORD-");
            model.Shipmnt_Tracking_No = new NumberFormatStore().NextNumber("Tracking", "TRK-", useGuidSuffix: true);

            var itemsJson = System.Text.Json.JsonSerializer.Serialize(model.LineItems.Select(l => new
            {
                l.Electronic_Id,
                l.Quantity,
                Unit_Price = l.Unit_Price,
                l.HSN_Code,
                l.Unit_Of_Measure,
                l.Discount_Type,
                l.Discount_Value,
                l.Discount_Amount,
                l.Tax_Percent,
                l.Tax_Amount,
                l.Line_Total
            }));

            var first = model.LineItems[0];
            var ao = new AddOrder
            {
                Order_No = model.Order_No,
                Order_Date = model.Order_Date,
                Payment_Type = model.Payment_Type,
                Payment_Status = model.Payment_Status,
                Order_Total_Cost = model.Order_Total_Cost,
                Shipping_Address = model.Shipping_Address,
                Delivery_Expected_Date = model.Delivery_Expected_Date,
                Shipmnt_Tracking_No = model.Shipmnt_Tracking_No,
                Shipmnt_Status = model.Shipmnt_Status,
                Warehouse_Name = model.Warehouse_Name,
                Shipping_Company = model.Shipping_Company,
                Customer_Name = model.Customer_Name,
                Customer_Email = model.Customer_Email,
                Customer_Phone = model.Customer_Phone,
                State_Id = model.State_Id,
                City_Id = model.City_Id,
                Pincode = model.Pincode,
                Order_Priority = model.Order_Priority,
                Order_Notes = model.Order_Notes,
                Electronic_Id = first.Electronic_Id,
                Quantity = model.LineItems.Sum(l => l.Quantity),
                Billing_Company = model.Billing_Company,
                Est_Dispatch_Date = model.Est_Dispatch_Date,
                Contractor = model.Contractor,
                PO_Number = model.PO_Number,
                PO_Date = model.PO_Date,
                Start_Date = model.Start_Date,
                Completion_Date = model.Completion_Date,
                Project_Title = model.Project_Title,
                Delivery_Terms = model.Delivery_Terms,
                Payment_Terms = model.Payment_Terms,
                Lead_Source = model.Lead_Source,
                Other_Source = model.Other_Source,
                ItemsJson = itemsJson
            };

            try
            {
                int result = ao.AddOrderDetails();
                if (result == -2)
                {
                    ModelState.AddModelError(string.Empty, "Insufficient stock for one or more products.");
                    LoadOrderLookups(model.State_Id);
                    return View(model);
                }

                if (result <= 0)
                {
                    ModelState.AddModelError(string.Empty, "Order could not be saved. Please try again or contact your administrator.");
                    LoadOrderLookups(model.State_Id);
                    return View(model);
                }

                TempData["OrderNo"] = model.Order_No;
                TempData["TrackingNo"] = model.Shipmnt_Tracking_No;
                return RedirectToAction(nameof(OrderSuccess));
            }
            catch (Exception ex)
            {
                ControllerError.AddModelError(this, _logger, ex,
                    "Create order failed for {UserId}",
                    User.Identity?.Name ?? "anonymous");
                LoadOrderLookups(model.State_Id);
                return View(model);
            }
        }

        public IActionResult OrderSuccess()
        {
            ViewData["Title"] = "Order Created";
            return View();
        }

        private void LoadOrderLookups(int? stateId = null)
        {
            ViewBag.States = ToSelect(new Lookup().GetStates(), "State_Id", "State_Name");
            ViewBag.Cities = stateId.HasValue && stateId > 0
                ? ToSelect(new Lookup().GetCitiesByState(stateId.Value), "City_Id", "City_Name")
                : new SelectList(new List<SelectListItem> { new("-- Select state first --", "") }, "Value", "Text");
            var products = new Lookup().GetProducts();
            ViewBag.Products = ProductSelect(products);
            ViewBag.ProductCatalogJson = ProductCatalogJson(products);
            ViewBag.Warehouses = WarehouseNameSelect(new Warehouse().GetAll());
            ViewBag.BillingCompanies = BillingCompanySelect();
            ViewBag.ShippingCompanies = ShippingCompanySelect();
            ViewBag.TaxPercents = new SelectList(new[]
            {
                new SelectListItem("0%", "0"),
                new SelectListItem("5%", "5"),
                new SelectListItem("12%", "12"),
                new SelectListItem("18%", "18"),
                new SelectListItem("28%", "28")
            }, "Value", "Text");
            try
            {
                var percents = new List<SelectListItem>();
                foreach (DataRow row in new FinanceMasterStore().GetTaxPercents().Rows)
                {
                    var rate = Convert.ToDecimal(row["RatePercent"]);
                    percents.Add(new SelectListItem(row["Label"]?.ToString() ?? $"{rate:0.##}%",
                        rate.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                }
                if (percents.Count > 0)
                    ViewBag.TaxPercents = new SelectList(percents, "Value", "Text");
            }
            catch { /* optional until SQL deployed */ }
        }

        private SelectList ShippingCompanySelect()
        {
            var items = new List<SelectListItem>();
            try
            {
                foreach (DataRow row in new Lookup().GetAppLookupItems("ShippingCompany").Rows)
                {
                    var label = row["Item_Label"]?.ToString() ?? "";
                    if (!string.IsNullOrWhiteSpace(label))
                        items.Add(new SelectListItem(label, label));
                }
            }
            catch (Exception ex)
            {
                ControllerError.CaptureWarning(this, _logger, ex,
                    "Failed to load shipping company lookups for {UserId}",
                    User.Identity?.Name ?? "anonymous");
            }

            if (items.Count == 0)
            {
                foreach (var name in new[] { "BlueDart", "Delhivery", "DTDC", "FedEx", "India Post", "Ecom Express" })
                    items.Add(new SelectListItem(name, name));
            }

            return new SelectList(items, "Value", "Text");
        }

        private SelectList BillingCompanySelect()
        {
            var items = new List<SelectListItem>();
            try
            {
                foreach (DataRow row in new CompanyStore().GetAll(activeOnly: true).Rows)
                {
                    var name = row["CompanyName"]?.ToString() ?? "";
                    if (!string.IsNullOrWhiteSpace(name))
                        items.Add(new SelectListItem(name, name));
                }
            }
            catch (Exception ex)
            {
                ControllerError.CaptureWarning(this, _logger, ex,
                    "Failed to load billing companies for {UserId}",
                    User.Identity?.Name ?? "anonymous");
            }
            return new SelectList(items, "Value", "Text");
        }

        private static string ProductCatalogJson(DataTable dt)
        {
            var list = new List<object>();
            foreach (DataRow row in dt.Rows)
            {
                var brand = row["Electronic_Brand"]?.ToString() ?? "";
                var name = row["Electronic_Name"]?.ToString() ?? "";
                var stock = row["Electronic_CRStock"] == DBNull.Value ? 0 : Convert.ToInt32(row["Electronic_CRStock"]);
                var price = row["Electronic_Price"] == DBNull.Value ? 0 : Convert.ToInt32(row["Electronic_Price"]);
                var hsn = row.Table.Columns.Contains("HSN_Code") ? row["HSN_Code"]?.ToString() ?? "" : "";
                var tax = row.Table.Columns.Contains("Tax_Percent") && row["Tax_Percent"] != DBNull.Value
                    ? Convert.ToDecimal(row["Tax_Percent"]) : 18m;
                list.Add(new
                {
                    id = Convert.ToInt32(row["Electronic_Id"]),
                    brand,
                    name,
                    stock,
                    price,
                    hsn,
                    tax,
                    label = $"{brand} · {name} (Stock: {stock})"
                });
            }
            return System.Text.Json.JsonSerializer.Serialize(list);
        }

        private static GetShipmentDetailsModel MapShipment(DataRow row)
        {
            DateTime orderDate = row["Order_Date"] != DBNull.Value
                ? Convert.ToDateTime(row["Order_Date"]) : DateTime.MinValue;
            DateTime delivery = row["Delivery_Expected_Date"] != DBNull.Value
                ? Convert.ToDateTime(row["Delivery_Expected_Date"]) : DateTime.MinValue;

            return new GetShipmentDetailsModel
            {
                Order_No = row["Order_No"]?.ToString(),
                Order_Date = DateOnly.FromDateTime(orderDate),
                Order_Total_Cost = row["Order_Total_Cost"] != DBNull.Value
                    ? Convert.ToDecimal(row["Order_Total_Cost"]) : 0,
                Delivery_Expected_Date = DateOnly.FromDateTime(delivery),
                Shipmnt_Status = row["Shipmnt_Status"]?.ToString(),
                Warehouse_Name = row["Warehouse_Name"]?.ToString(),
                Shipping_Company = row["Shipping_Company"]?.ToString(),
                Shipmnt_Tracking_No = row["Shipmnt_Tracking_No"]?.ToString(),
                Customer_Name = Col(row, "Customer_Name"),
                Customer_Email = Col(row, "Customer_Email"),
                Customer_Phone = Col(row, "Customer_Phone"),
                Shipping_Address = Col(row, "Shipping_Address"),
                State_Name = Col(row, "State_Name"),
                City_Name = Col(row, "City_Name"),
                Pincode = Col(row, "Pincode"),
                Order_Priority = Col(row, "Order_Priority"),
                Order_Notes = Col(row, "Order_Notes"),
                Product_Name = Col(row, "Product_Name"),
                Quantity = row.Table.Columns.Contains("Quantity") && row["Quantity"] != DBNull.Value
                    ? Convert.ToInt32(row["Quantity"]) : 0
            };
        }

        private static string? Col(DataRow row, string name) =>
            row.Table.Columns.Contains(name) ? row[name]?.ToString() : null;

        private static SelectList ToSelect(DataTable dt, string value, string text)
        {
            var items = new List<SelectListItem> { new("-- Select --", "") };
            foreach (DataRow row in dt.Rows)
                items.Add(new SelectListItem(row[text]?.ToString() ?? "", row[value]?.ToString()));
            return new SelectList(items, "Value", "Text");
        }

        private static SelectList ProductSelect(DataTable dt)
        {
            var items = new List<SelectListItem> { new("-- Select product --", "") };
            foreach (DataRow row in dt.Rows)
            {
                var stock = Convert.ToInt32(row["Electronic_CRStock"]);
                var label = $"{row["Electronic_Brand"]} · {row["Electronic_Name"]} (Stock: {stock})";
                items.Add(new SelectListItem(label, row["Electronic_Id"]?.ToString()));
            }
            return new SelectList(items, "Value", "Text");
        }

        private static SelectList WarehouseNameSelect(DataTable dt)
        {
            var items = new List<SelectListItem> { new("-- Select warehouse --", "") };
            foreach (DataRow row in dt.Rows)
            {
                var name = row["Warehouse_Name"]?.ToString() ?? "";
                var city = row.Table.Columns.Contains("City_Name") ? row["City_Name"]?.ToString() : null;
                var label = string.IsNullOrWhiteSpace(city) ? name : $"{name} · {city}";
                items.Add(new SelectListItem(label, name));
            }
            return new SelectList(items, "Value", "Text");
        }
    }
}
