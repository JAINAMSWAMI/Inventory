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
    public class DocumentsController : Controller
    {
        private readonly ILogger<DocumentsController> _logger;

        public DocumentsController(ILogger<DocumentsController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            ViewData["Title"] = "GRN & DCN";
            var grn = new GRNModel();
            var dcn = new DCNModel();
            try
            {
                foreach (DataRow row in new InventoryDocuments().GetAllGRN().Rows)
                {
                    grn.Items.Add(new GRNListItem
                    {
                        GRN_Id = Convert.ToInt32(row["GRN_Id"]),
                        GRN_No = row["GRN_No"]?.ToString(),
                        GRN_Date = Convert.ToDateTime(row["GRN_Date"]),
                        Party_Type = row["Party_Type"]?.ToString(),
                        Supplier_Name = Col(row, "Supplier_Name"),
                        From_Warehouse_Name = Col(row, "From_Warehouse_Name"),
                        To_Warehouse_Name = Col(row, "To_Warehouse_Name"),
                        Invoice_No = Col(row, "Invoice_No"),
                        Status = Col(row, "Status"),
                        TotalQty = SafeInt(row, "TotalQty"),
                        Received_By = Col(row, "Received_By")
                    });
                }

                foreach (DataRow row in new InventoryDocuments().GetAllDCN().Rows)
                {
                    dcn.Items.Add(new DCNListItem
                    {
                        DCN_Id = Convert.ToInt32(row["DCN_Id"]),
                        DCN_No = row["DCN_No"]?.ToString(),
                        DCN_Date = Convert.ToDateTime(row["DCN_Date"]),
                        Party_Type = row["Party_Type"]?.ToString(),
                        Customer_Name = Col(row, "Customer_Name"),
                        From_Warehouse_Name = Col(row, "From_Warehouse_Name"),
                        To_Warehouse_Name = Col(row, "To_Warehouse_Name"),
                        Order_No = Col(row, "Order_No"),
                        Status = Col(row, "Status"),
                        TotalQty = SafeInt(row, "TotalQty")
                    });
                }
            }
            catch (Exception ex)
            {
                ControllerError.Capture(this, _logger, ex,
                    "Failed loading GRN/DCN for {UserId}",
                    User.Identity?.Name ?? "anonymous");
            }

            ViewBag.DCN = dcn;
            return View(grn);
        }

        [HttpGet]
        public IActionResult CreateGRN()
        {
            ViewData["Title"] = "Create GRN";
            LoadLookups();
            return View(new GRNModel
            {
                Received_By = User.Identity?.Name,
                LineItems = { new GRNLineItemModel() }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateGRN(GRNModel model)
        {
            model.LineItems ??= new List<GRNLineItemModel>();
            model.LineItems = model.LineItems
                .Where(l => l.Electronic_Id > 0 || l.Quantity > 0 || !string.IsNullOrWhiteSpace(l.Product_Label))
                .ToList();

            if (model.Party_Type == "Vendor" && !model.Supplier_Id.HasValue)
                ModelState.AddModelError(nameof(model.Supplier_Id), "Select a vendor.");
            if (model.Party_Type == "Warehouse" && !model.From_Warehouse_Id.HasValue)
                ModelState.AddModelError(nameof(model.From_Warehouse_Id), "Select source warehouse.");
            if (!model.To_Warehouse_Id.HasValue)
                ModelState.AddModelError(nameof(model.To_Warehouse_Id), "Select receiving warehouse.");

            if (model.LineItems.Count == 0)
                ModelState.AddModelError(string.Empty, "Add at least one product line.");

            for (var i = 0; i < model.LineItems.Count; i++)
            {
                var line = model.LineItems[i];
                if (line.Electronic_Id <= 0)
                    ModelState.AddModelError($"LineItems[{i}].Electronic_Id", "Select a product.");
                if (line.Quantity <= 0)
                    ModelState.AddModelError($"LineItems[{i}].Quantity", "Quantity must be at least 1.");
            }

            var duplicateIds = model.LineItems
                .Where(l => l.Electronic_Id > 0)
                .GroupBy(l => l.Electronic_Id)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            if (duplicateIds.Count > 0)
                ModelState.AddModelError(string.Empty, "Each product can only appear once. Combine quantities on a single line.");

            if (!ModelState.IsValid)
            {
                if (model.LineItems.Count == 0)
                    model.LineItems.Add(new GRNLineItemModel());
                LoadLookups();
                return View(model);
            }

            try
            {
                var itemsJson = System.Text.Json.JsonSerializer.Serialize(
                    model.LineItems.Select(l => new
                    {
                        l.Electronic_Id,
                        l.Quantity,
                        Unit_Cost = l.Unit_Cost,
                        Batch_No = l.Batch_No,
                        Expiry_Date = l.Expiry_Date?.ToString("yyyy-MM-dd")
                    }));

                var dl = new InventoryDocuments
                {
                    GRN_No = new NumberFormatStore().NextNumber("GRN", "GRN-"),
                    GRN_Date = model.GRN_Date,
                    Party_Type = model.Party_Type,
                    Supplier_Id = model.Party_Type == "Vendor" ? model.Supplier_Id : null,
                    From_Warehouse_Id = model.Party_Type == "Warehouse" ? model.From_Warehouse_Id : null,
                    To_Warehouse_Id = model.To_Warehouse_Id,
                    Invoice_No = model.Invoice_No,
                    Invoice_Date = model.Invoice_Date,
                    Vehicle_No = model.Vehicle_No,
                    Gate_Entry_No = model.Gate_Entry_No,
                    Received_By = model.Received_By ?? User.Identity?.Name,
                    Remarks = model.Remarks,
                    ItemsJson = itemsJson
                };

                if (dl.InsertGRN() > 0)
                {
                    TempData["Success"] = $"GRN {dl.GRN_No} posted with {model.LineItems.Count} product(s). Stock increased.";
                    return RedirectToAction(nameof(Index));
                }

                if (!string.IsNullOrWhiteSpace(dl.LastError))
                    _logger.LogWarning("InsertGRN failed: {Error}", dl.LastError);

                ModelState.AddModelError(string.Empty,
                    string.IsNullOrWhiteSpace(dl.LastError)
                        ? "GRN could not be saved."
                        : "Could not save GRN. Please try again or contact your administrator.");
            }
            catch (Exception ex)
            {
                ControllerError.AddModelError(this, _logger, ex,
                    "GRN save failed for {UserId}",
                    User.Identity?.Name ?? "anonymous");
            }

            LoadLookups();
            return View(model);
        }

        [HttpGet]
        public IActionResult CreateDCN()
        {
            ViewData["Title"] = "Create DCN";
            LoadLookups();
            return View(new DCNModel
            {
                Delivered_By = User.Identity?.Name,
                LineItems = { new DCNLineItemModel() }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateDCN(DCNModel model)
        {
            model.LineItems ??= new List<DCNLineItemModel>();
            model.LineItems = model.LineItems
                .Where(l => l.Electronic_Id > 0 || l.Quantity > 0)
                .ToList();

            if (model.Party_Type == "Customer" && string.IsNullOrWhiteSpace(model.Customer_Name))
                ModelState.AddModelError(nameof(model.Customer_Name), "Customer name is required.");
            if (model.Party_Type == "Warehouse" && !model.To_Warehouse_Id.HasValue)
                ModelState.AddModelError(nameof(model.To_Warehouse_Id), "Select destination warehouse.");
            if (model.Party_Type == "Warehouse" && !model.From_Warehouse_Id.HasValue)
                ModelState.AddModelError(nameof(model.From_Warehouse_Id), "Select source warehouse.");

            if (model.Party_Type == "Warehouse")
            {
                if (model.Electronic_Id <= 0)
                    ModelState.AddModelError(nameof(model.Electronic_Id), "Select a product.");
                if (model.Quantity <= 0)
                    ModelState.AddModelError(nameof(model.Quantity), "Quantity must be at least 1.");
            }
            else
            {
                if (model.LineItems.Count == 0)
                    ModelState.AddModelError(string.Empty, "Select a linked order or add at least one product.");
                for (var i = 0; i < model.LineItems.Count; i++)
                {
                    if (model.LineItems[i].Electronic_Id <= 0)
                        ModelState.AddModelError($"LineItems[{i}].Electronic_Id", "Select a product.");
                    if (model.LineItems[i].Quantity <= 0)
                        ModelState.AddModelError($"LineItems[{i}].Quantity", "Enter quantity.");
                }
            }

            ModelState.Remove(nameof(model.Electronic_Id));
            ModelState.Remove(nameof(model.Quantity));
            if (model.Party_Type == "Customer")
                ModelState.Remove(nameof(model.From_Warehouse_Id));

            if (!ModelState.IsValid)
            {
                if (model.LineItems.Count == 0) model.LineItems.Add(new DCNLineItemModel());
                LoadLookups();
                return View(model);
            }

            try
            {
                string? itemsJson = null;
                if (model.Party_Type == "Customer")
                {
                    itemsJson = System.Text.Json.JsonSerializer.Serialize(model.LineItems.Select(l => new
                    {
                        l.Electronic_Id,
                        l.Quantity,
                        Unit_Price = l.Unit_Price
                    }));
                }

                var dl = new InventoryDocuments
                {
                    DCN_No = new NumberFormatStore().NextNumber("DCN", "DCN-"),
                    DCN_Date = model.DCN_Date,
                    Party_Type = model.Party_Type,
                    Customer_Name = model.Party_Type == "Customer" ? model.Customer_Name : null,
                    Customer_Phone = model.Customer_Phone,
                    Customer_Address = model.Customer_Address,
                    Customer_Email = model.Customer_Email,
                    Customer_GSTIN = model.Customer_GSTIN,
                    Billing_Company = model.Billing_Company,
                    PO_Number = model.PO_Number,
                    Pincode = model.Pincode,
                    From_Warehouse_Id = model.From_Warehouse_Id,
                    To_Warehouse_Id = model.Party_Type == "Warehouse" ? model.To_Warehouse_Id : null,
                    Order_No = model.Order_No,
                    Vehicle_No = model.Vehicle_No,
                    Transporter = model.Transporter,
                    Delivered_By = model.Delivered_By ?? User.Identity?.Name,
                    Remarks = model.Remarks,
                    Electronic_Id = model.Party_Type == "Warehouse" ? model.Electronic_Id : 0,
                    Quantity = model.Party_Type == "Warehouse" ? model.Quantity : 0,
                    Unit_Price = model.Unit_Price,
                    ItemsJson = itemsJson
                };

                var result = dl.InsertDCN();
                if (result == -2)
                {
                    ModelState.AddModelError(string.Empty,
                        "Insufficient stock across open batches (FIFO). Reduce quantity or receive stock via GRN first.");
                    LoadLookups();
                    return View(model);
                }

                if (result > 0)
                {
                    TempData["Success"] = model.Party_Type == "Warehouse"
                        ? $"DCN {dl.DCN_No} posted — warehouse transfer logged."
                        : $"DCN {dl.DCN_No} posted with {model.LineItems.Count} product(s) — FIFO COGS applied.";
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError(string.Empty, "DCN could not be saved.");
            }
            catch (Exception ex)
            {
                ControllerError.AddModelError(this, _logger, ex,
                    "DCN save failed for {UserId}",
                    User.Identity?.Name ?? "anonymous");
            }

            LoadLookups();
            return View(model);
        }

        [HttpGet]
        public IActionResult SearchCustomers(string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 3)
                return Json(new { ok = true, items = Array.Empty<object>() });
            try
            {
                var dt = new SalesLookup().SearchCustomers(q.Trim());
                var items = new List<object>();
                foreach (DataRow r in dt.Rows)
                {
                    items.Add(new
                    {
                        name = r["Customer_Name"]?.ToString(),
                        email = Col(r, "Customer_Email"),
                        phone = Col(r, "Customer_Phone"),
                        address = Col(r, "Shipping_Address"),
                        billing = Col(r, "Billing_Company"),
                        pincode = Col(r, "Pincode")
                    });
                }
                return Json(new { ok = true, items });
            }
            catch (Exception ex)
            {
                return Json(ControllerError.JsonError(this, _logger, ex,
                    "SearchCustomers failed for query length {QueryLength}",
                    q?.Length ?? 0));
            }
        }

        [HttpGet]
        public IActionResult OrdersByCustomer(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Json(new { ok = true, orders = Array.Empty<object>() });
            try
            {
                var dt = new SalesLookup().GetOrdersByCustomer(name.Trim());
                var orders = new List<object>();
                foreach (DataRow r in dt.Rows)
                {
                    orders.Add(new
                    {
                        orderNo = r["Order_No"]?.ToString(),
                        orderDate = Convert.ToDateTime(r["Order_Date"]).ToString("dd MMM yyyy"),
                        status = r["Shipmnt_Status"]?.ToString(),
                        total = r["Order_Total_Cost"] == DBNull.Value ? 0 : Convert.ToDecimal(r["Order_Total_Cost"]),
                        qty = SafeInt(r, "TotalQty")
                    });
                }
                return Json(new { ok = true, orders });
            }
            catch (Exception ex)
            {
                return Json(ControllerError.JsonError(this, _logger, ex,
                    "OrdersByCustomer failed for customer {CustomerName}",
                    name));
            }
        }

        [HttpGet]
        public IActionResult OrderItems(string orderNo)
        {
            if (string.IsNullOrWhiteSpace(orderNo))
                return Json(new { ok = true, items = Array.Empty<object>(), header = (object?)null });
            try
            {
                var dt = new SalesLookup().GetOrderItemsByOrderNo(orderNo.Trim());
                object? header = null;
                var items = new List<object>();
                foreach (DataRow r in dt.Rows)
                {
                    header ??= new
                    {
                        orderNo = r["Order_No"]?.ToString(),
                        customer = Col(r, "Customer_Name"),
                        email = Col(r, "Customer_Email"),
                        phone = Col(r, "Customer_Phone"),
                        address = Col(r, "Shipping_Address"),
                        billing = Col(r, "Billing_Company"),
                        po = Col(r, "PO_Number"),
                        pincode = Col(r, "Pincode")
                    };
                    var brand = Col(r, "Electronic_Brand") ?? "";
                    var name = Col(r, "Product_Name") ?? "";
                    var stock = SafeInt(r, "Electronic_CRStock");
                    items.Add(new
                    {
                        electronicId = Convert.ToInt32(r["Electronic_Id"]),
                        quantity = Convert.ToInt32(r["Quantity"]),
                        unitPrice = r["Unit_Price"] == DBNull.Value ? 0m : Convert.ToDecimal(r["Unit_Price"]),
                        label = $"{brand} · {name} (Stock: {stock})"
                    });
                }
                return Json(new { ok = true, items, header });
            }
            catch (Exception ex)
            {
                return Json(ControllerError.JsonError(this, _logger, ex,
                    "OrderItems failed for order {OrderNo}",
                    orderNo));
            }
        }

        [HttpGet]
        public IActionResult OpenBatches(int electronicId)
        {
            if (electronicId <= 0)
                return Json(new { ok = false, batches = Array.Empty<object>() });

            try
            {
                var dt = new InventoryDocuments().GetProductOpenBatches(electronicId);
                var batches = new List<object>();
                foreach (DataRow row in dt.Rows)
                {
                    batches.Add(new
                    {
                        batchId = Convert.ToInt32(row["Batch_Id"]),
                        batchNo = row["Batch_No"]?.ToString(),
                        grnDate = Convert.ToDateTime(row["GRN_Date"]).ToString("dd MMM yyyy"),
                        remaining = Convert.ToInt32(row["Remaining_Qty"]),
                        unitCost = Convert.ToDecimal(row["Unit_Cost"])
                    });
                }
                return Json(new { ok = true, batches });
            }
            catch (Exception ex)
            {
                return Json(ControllerError.JsonError(this, _logger, ex,
                    "OpenBatches failed for product {ElectronicId}",
                    electronicId));
            }
        }

        private void LoadLookups()
        {
            var products = new Lookup().GetProducts();
            ViewBag.Products = ProductSelect(products);
            ViewBag.ProductCatalogJson = ProductCatalogJson(products);
            ViewBag.Suppliers = ToSelect(new Supplier().GetAll(), "Supplier_Id", "Supplier_Name");
            ViewBag.Warehouses = ToSelect(new Warehouse().GetAll(), "Warehouse_Id", "Warehouse_Name");
            ViewBag.BillingCompanies = BillingCompanySelect();
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
                    "Failed to load billing companies for documents for {UserId}",
                    User.Identity?.Name ?? "anonymous");
            }
            return new SelectList(items, "Value", "Text");
        }

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
                var label = $"{row["Electronic_Brand"]} · {row["Electronic_Name"]} (Stock: {row["Electronic_CRStock"]})";
                items.Add(new SelectListItem(label, row["Electronic_Id"]?.ToString()));
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
                var stock = row["Electronic_CRStock"];
                var id = Convert.ToInt32(row["Electronic_Id"]);
                list.Add(new
                {
                    id,
                    brand,
                    name,
                    stock = stock == DBNull.Value ? 0 : Convert.ToInt32(stock),
                    label = $"{brand} · {name} (Stock: {stock})"
                });
            }
            return System.Text.Json.JsonSerializer.Serialize(list);
        }

        private static string? Col(DataRow row, string name) =>
            row.Table.Columns.Contains(name) ? row[name]?.ToString() : null;

        private static int SafeInt(DataRow row, string col) =>
            row.Table.Columns.Contains(col) && row[col] != DBNull.Value ? Convert.ToInt32(row[col]) : 0;
    }
}
