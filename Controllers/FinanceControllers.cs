using DataLayer;
using Inventory.Infrastructure;
using Inventory.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;
using System.Globalization;

namespace Inventory.Controllers
{
    [Authorize]
    public class InvoiceController : Controller
    {
        private readonly ILogger<InvoiceController> _logger;

        public InvoiceController(ILogger<InvoiceController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            ViewData["Title"] = "Invoices";
            var list = new List<InvoiceListItem>();
            try
            {
                foreach (DataRow row in new InvoiceStore().GetAll().Rows)
                    list.Add(MapList(row));
            }
            catch (Exception ex)
            {
                ControllerError.Capture(this, _logger, ex,
                    "Failed to load invoices for {UserId}",
                    User.Identity?.Name ?? "anonymous");
            }
            return View(list);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewData["Title"] = "Create Invoice";
            var model = new InvoiceCreateModel
            {
                Invoice_No = PreviewInvoiceNo(),
                Assignee = User.Identity?.Name,
                Due_Date = DateTime.Today.AddDays(15)
            };
            LoadLookups();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(InvoiceCreateModel model)
        {
            ViewData["Title"] = "Create Invoice";
            model.LineItems ??= new List<InvoiceLineItemModel>();
            model.LineItems = model.LineItems
                .Where(l => !string.IsNullOrWhiteSpace(l.Product_Label) && l.Quantity > 0)
                .ToList();

            if (model.LineItems.Count == 0)
                ModelState.AddModelError(string.Empty, "Add at least one line item.");

            if (!ModelState.IsValid)
            {
                if (model.LineItems.Count == 0) model.LineItems.Add(new InvoiceLineItemModel());
                LoadLookups(model.Ship_State_Id);
                return View(model);
            }

            try
            {
                var amount = model.LineItems.Sum(l =>
                    l.Line_Total > 0 ? l.Line_Total : (l.Quantity * l.Unit_Price + l.Tax_Amount));
                var itemsJson = System.Text.Json.JsonSerializer.Serialize(model.LineItems.Select(l => new
                {
                    l.Electronic_Id,
                    l.Product_Label,
                    l.Quantity,
                    l.Unit_Price,
                    l.HSN_Code,
                    l.Unit_Of_Measure,
                    l.Discount_Type,
                    l.Discount_Value,
                    l.Discount_Amount,
                    l.Tax_Percent,
                    l.Tax_Amount,
                    l.Line_Total
                }));

                var store = new InvoiceStore
                {
                    Invoice_No = new NumberFormatStore().NextNumber("Invoice", "INV-"),
                    Invoice_Type = model.Invoice_Type,
                    Invoice_Date = model.Invoice_Date,
                    Due_Date = model.Due_Date,
                    Status = model.Status,
                    Payment_Status = model.Payment_Status,
                    Business_Segment = model.Business_Segment,
                    Business_Category = model.Business_Category,
                    Business_SubCategory = model.Business_SubCategory,
                    Customer_Id = model.Customer_Id,
                    Business_Name = model.Business_Name.Trim(),
                    Contact_Title = model.Contact_Title,
                    Contact_Name = model.Contact_Name,
                    Contact_Phone = model.Contact_Phone,
                    Billing_Company = model.Billing_Company,
                    PO_Number = model.PO_Number,
                    PO_Date = model.PO_Date,
                    Project_Title = model.Project_Title,
                    Delivery_Terms = model.Delivery_Terms,
                    Payment_Terms = model.Payment_Terms,
                    Warehouse_Name = model.Warehouse_Name,
                    Linked_Order_No = model.Linked_Order_No,
                    Assignee = model.Assignee ?? User.Identity?.Name,
                    Ship_Same_As_Bill = model.Ship_Same_As_Bill,
                    Ship_Consignee = model.Ship_Consignee,
                    Ship_Contact_Name = model.Ship_Contact_Name,
                    Ship_Contact_Phone = model.Ship_Contact_Phone,
                    Ship_Address1 = model.Ship_Address1,
                    Ship_Address2 = model.Ship_Address2,
                    Ship_State_Id = model.Ship_State_Id,
                    Ship_City_Id = model.Ship_City_Id,
                    Ship_Pincode = model.Ship_Pincode,
                    Ship_GSTIN = model.Ship_GSTIN,
                    Amount = amount,
                    Remarks = model.Remarks,
                    Created_By = User.Identity?.Name,
                    ItemsJson = itemsJson
                };

                var invoiceId = store.Insert();
                if (invoiceId > 0)
                {
                    TempData["Success"] = $"Invoice {store.Invoice_No} saved. Preview is ready.";
                    return RedirectToAction(nameof(Preview), new { id = invoiceId });
                }

                TempData["Error"] = string.IsNullOrWhiteSpace(store.LastError)
                    ? "Could not save invoice."
                    : store.LastError;
            }
            catch (Exception ex)
            {
                ControllerError.Capture(this, _logger, ex,
                    "Create invoice failed for {UserId}",
                    User.Identity?.Name ?? "anonymous");
            }

            LoadLookups(model.Ship_State_Id);
            return View(model);
        }

        [HttpGet]
        public IActionResult Preview(int id)
        {
            ViewData["Title"] = "Invoice Preview";
            try
            {
                var (header, items) = new InvoiceStore().GetById(id);
                if (header.Rows.Count == 0)
                {
                    TempData["Error"] = "Invoice not found.";
                    return RedirectToAction(nameof(Index));
                }

                var row = header.Rows[0];
                var model = new InvoicePreviewModel
                {
                    Invoice_Id = id,
                    Invoice_No = row["Invoice_No"]?.ToString() ?? "",
                    Invoice_Type = row["Invoice_Type"]?.ToString() ?? "Tax",
                    Invoice_Date = Convert.ToDateTime(row["Invoice_Date"]),
                    Due_Date = row["Due_Date"] == DBNull.Value ? null : Convert.ToDateTime(row["Due_Date"]),
                    Status = row["Status"]?.ToString() ?? "",
                    Payment_Status = row["Payment_Status"]?.ToString() ?? "",
                    Business_Name = row["Business_Name"]?.ToString() ?? "",
                    Contact_Name = row["Contact_Name"]?.ToString(),
                    Contact_Phone = row["Contact_Phone"]?.ToString(),
                    Billing_Company = row["Billing_Company"]?.ToString(),
                    Company_LogoUrl = header.Columns.Contains("Company_LogoUrl") ? row["Company_LogoUrl"]?.ToString() : null,
                    Company_Gstin = header.Columns.Contains("Company_Gstin") ? row["Company_Gstin"]?.ToString() : null,
                    Company_Email = header.Columns.Contains("Company_Email") ? row["Company_Email"]?.ToString() : null,
                    Company_Phone = header.Columns.Contains("Company_Phone") ? row["Company_Phone"]?.ToString() : null,
                    Company_Address = header.Columns.Contains("Company_Address") ? row["Company_Address"]?.ToString() : null,
                    PO_Number = row["PO_Number"]?.ToString(),
                    Project_Title = row["Project_Title"]?.ToString(),
                    Payment_Terms = row["Payment_Terms"]?.ToString(),
                    Delivery_Terms = row["Delivery_Terms"]?.ToString(),
                    Ship_Address1 = row["Ship_Address1"]?.ToString(),
                    Ship_Address2 = row["Ship_Address2"]?.ToString(),
                    Ship_Pincode = row["Ship_Pincode"]?.ToString(),
                    Ship_GSTIN = row["Ship_GSTIN"]?.ToString(),
                    Remarks = row["Remarks"]?.ToString(),
                    Amount = row["Amount"] == DBNull.Value ? 0 : Convert.ToDecimal(row["Amount"])
                };

                foreach (DataRow li in items.Rows)
                {
                    model.LineItems.Add(new InvoiceLineItemModel
                    {
                        Electronic_Id = li["Electronic_Id"] == DBNull.Value ? null : Convert.ToInt32(li["Electronic_Id"]),
                        Product_Label = li["Product_Label"]?.ToString() ?? "",
                        HSN_Code = items.Columns.Contains("HSN_Code") ? li["HSN_Code"]?.ToString() : null,
                        Unit_Of_Measure = items.Columns.Contains("Unit_Of_Measure") ? li["Unit_Of_Measure"]?.ToString() ?? "Pcs" : "Pcs",
                        Quantity = Convert.ToDecimal(li["Quantity"]),
                        Unit_Price = Convert.ToDecimal(li["Unit_Price"]),
                        Discount_Type = items.Columns.Contains("Discount_Type") ? li["Discount_Type"]?.ToString() ?? "Percent" : "Percent",
                        Discount_Value = items.Columns.Contains("Discount_Value") && li["Discount_Value"] != DBNull.Value ? Convert.ToDecimal(li["Discount_Value"]) : 0,
                        Discount_Amount = items.Columns.Contains("Discount_Amount") && li["Discount_Amount"] != DBNull.Value ? Convert.ToDecimal(li["Discount_Amount"]) : 0,
                        Tax_Percent = items.Columns.Contains("Tax_Percent") && li["Tax_Percent"] != DBNull.Value ? Convert.ToDecimal(li["Tax_Percent"]) : 0,
                        Tax_Amount = items.Columns.Contains("Tax_Amount") && li["Tax_Amount"] != DBNull.Value ? Convert.ToDecimal(li["Tax_Amount"]) : 0,
                        Line_Total = items.Columns.Contains("Line_Total") && li["Line_Total"] != DBNull.Value
                            ? Convert.ToDecimal(li["Line_Total"])
                            : Convert.ToDecimal(li["Quantity"]) * Convert.ToDecimal(li["Unit_Price"])
                    });
                }

                return View(model);
            }
            catch (Exception ex)
            {
                ControllerError.Capture(this, _logger, ex, "Invoice preview failed for {InvoiceId}", id);
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public IActionResult SearchBusiness(string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
                return Json(Array.Empty<object>());

            try
            {
                var list = new List<object>();
                foreach (DataRow row in new CustomerStore().Search(q.Trim()).Rows)
                {
                    list.Add(new
                    {
                        id = row["Customer_Id"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["Customer_Id"]),
                        name = row["Business_Name"]?.ToString(),
                        contactTitle = row["Contact_Title"]?.ToString(),
                        contactName = row["Contact_Name"]?.ToString(),
                        phone = row["Contact_Phone"]?.ToString(),
                        email = row["Email"]?.ToString(),
                        address1 = row["Address1"]?.ToString(),
                        address2 = row["Address2"]?.ToString(),
                        stateId = row["State_Id"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["State_Id"]),
                        cityId = row["City_Id"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["City_Id"]),
                        pincode = row["Pincode"]?.ToString(),
                        gstin = row["GSTIN"]?.ToString()
                    });
                }
                return Json(list);
            }
            catch (Exception ex)
            {
                return Json(ControllerError.JsonError(this, _logger, ex,
                    "SearchBusiness failed for query length {QueryLength}",
                    q?.Length ?? 0));
            }
        }

        private void LoadLookups(int? stateId = null)
        {
            ViewBag.States = ToSelect(new Lookup().GetStates(), "State_Id", "State_Name");
            ViewBag.Cities = stateId.HasValue && stateId > 0
                ? ToSelect(new Lookup().GetCitiesByState(stateId.Value), "City_Id", "City_Name")
                : new SelectList(new List<SelectListItem> { new("-- Select state first --", "") }, "Value", "Text");

            var warehouses = new List<SelectListItem> { new("-- Select warehouse --", "") };
            try
            {
                foreach (DataRow row in new Warehouse().GetAll().Rows)
                    warehouses.Add(new SelectListItem(row["Warehouse_Name"]?.ToString() ?? "", row["Warehouse_Name"]?.ToString()));
            }
            catch (Exception ex)
            {
                ControllerError.CaptureWarning(this, _logger, ex,
                    "Failed to load warehouses for invoice form for {UserId}",
                    User.Identity?.Name ?? "anonymous");
            }
            ViewBag.Warehouses = new SelectList(warehouses, "Value", "Text");

            var companies = new List<SelectListItem> { new("-- Select billing company --", "") };
            try
            {
                foreach (DataRow row in new CompanyStore().GetAll(activeOnly: true).Rows)
                {
                    var name = row["CompanyName"]?.ToString() ?? "";
                    if (!string.IsNullOrWhiteSpace(name))
                        companies.Add(new SelectListItem(name, name));
                }
            }
            catch (Exception ex)
            {
                ControllerError.CaptureWarning(this, _logger, ex,
                    "Failed to load billing companies for invoice form for {UserId}",
                    User.Identity?.Name ?? "anonymous");
            }
            ViewBag.BillingCompanies = new SelectList(companies, "Value", "Text");

            ViewBag.Segments = new SelectList(new[] { "Product Sales", "Services", "Spare Parts" });
            ViewBag.Categories = new SelectList(new[] { "B2B", "B2C", "Government", "Export" });
            ViewBag.Statuses = new SelectList(new[] { "Draft", "Submitted", "Approved", "Cancelled" });
            ViewBag.PaymentStatuses = new SelectList(new[] { "Unpaid", "Partial", "Paid" });
            ViewBag.DeliveryTerms = new SelectList(new[] { "Ex-Works", "FOB", "CIF", "Door Delivery" });
            ViewBag.PaymentTerms = LookupOrDefault("PaymentTerms", new[] { "Immediate", "Net 15", "Net 30", "Net 45" });
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
                ViewBag.ProductCatalogJson = BuildProductCatalogJson(new Lookup().GetProducts());
            }
            catch (Exception ex)
            {
                ControllerError.CaptureWarning(this, _logger, ex,
                    "Failed to load product catalog for invoice for {UserId}",
                    User.Identity?.Name ?? "anonymous");
                ViewBag.ProductCatalogJson = "[]";
            }
        }

        private static string BuildProductCatalogJson(DataTable dt)
        {
            var list = new List<object>();
            foreach (DataRow row in dt.Rows)
            {
                var brand = row["Electronic_Brand"]?.ToString() ?? "";
                var name = row["Electronic_Name"]?.ToString() ?? "";
                var stock = row["Electronic_CRStock"] == DBNull.Value ? 0 : Convert.ToInt32(row["Electronic_CRStock"]);
                var price = row["Electronic_Price"] == DBNull.Value ? 0 : Convert.ToDecimal(row["Electronic_Price"]);
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

        private SelectList LookupOrDefault(string key, string[] fallback)
        {
            var items = new List<string>();
            try
            {
                foreach (DataRow row in new Lookup().GetAppLookupItems(key).Rows)
                {
                    var label = row["Item_Label"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(label)) items.Add(label!);
                }
            }
            catch (Exception ex)
            {
                ControllerError.CaptureWarning(this, _logger, ex,
                    "Failed to load lookup {LookupKey} for invoice form",
                    key);
            }
            if (items.Count == 0) items.AddRange(fallback);
            return new SelectList(items);
        }

        private static SelectList ToSelect(DataTable dt, string value, string text)
        {
            var items = new List<SelectListItem> { new("-- Select --", "") };
            foreach (DataRow row in dt.Rows)
                items.Add(new SelectListItem(row[text]?.ToString() ?? "", row[value]?.ToString()));
            return new SelectList(items, "Value", "Text");
        }

        private string PreviewInvoiceNo()
        {
            try
            {
                var store = new AppSettingsStore();
                var formats = new NumberFormatStore().GetAll();
                foreach (DataRow row in formats.Rows)
                {
                    if (!string.Equals(row["Form_Key"]?.ToString(), "Invoice", StringComparison.OrdinalIgnoreCase))
                        continue;
                    var prefix = NumberFormatStore.ExpandTokens(row["Prefix"]?.ToString() ?? "INV-");
                    var next = Convert.ToInt32(row["Next_Number"]);
                    return prefix + next + " (preview)";
                }
            }
            catch (Exception ex)
            {
                ControllerError.CaptureWarning(this, _logger, ex,
                    "Failed to preview invoice number for {UserId}",
                    User.Identity?.Name ?? "anonymous");
            }
            return "Auto-generated on save";
        }

        private static InvoiceListItem MapList(DataRow row) => new()
        {
            Invoice_Id = Convert.ToInt32(row["Invoice_Id"]),
            Invoice_No = row["Invoice_No"]?.ToString() ?? "",
            Business_Name = row["Business_Name"]?.ToString() ?? "",
            Project_Title = row["Project_Title"]?.ToString(),
            First_Item_Label = row.Table.Columns.Contains("First_Item_Label") ? row["First_Item_Label"]?.ToString() : null,
            Linked_Order_No = row["Linked_Order_No"]?.ToString(),
            Business_Segment = row["Business_Segment"]?.ToString(),
            Business_Category = row["Business_Category"]?.ToString(),
            Status = row["Status"]?.ToString() ?? "Draft",
            Payment_Status = row["Payment_Status"]?.ToString() ?? "Unpaid",
            Amount = row["Amount"] != DBNull.Value ? Convert.ToDecimal(row["Amount"]) : 0,
            Received_Amount = row["Received_Amount"] != DBNull.Value ? Convert.ToDecimal(row["Received_Amount"]) : 0,
            Assignee = row["Assignee"]?.ToString(),
            Invoice_Date = row["Invoice_Date"] != DBNull.Value ? Convert.ToDateTime(row["Invoice_Date"]) : DateTime.MinValue,
            Created_By = row["Created_By"]?.ToString(),
            Created_At = row["Created_At"] != DBNull.Value ? Convert.ToDateTime(row["Created_At"]) : DateTime.MinValue
        };
    }


}

