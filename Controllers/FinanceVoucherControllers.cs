using DataLayer;
using Inventory.Infrastructure;
using Inventory.Models;
using Inventory.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;
using System.Security.Claims;
using System.Text.Json;

namespace Inventory.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly ILogger<PaymentController> _logger;
        private readonly IWebHostEnvironment _env;

        public PaymentController(ILogger<PaymentController> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        [HttpGet]
        public IActionResult Receipts()
        {
            ViewData["Title"] = "Payments / Receipts";
            var list = new List<FinanceVoucherListItem>();
            try
            {
                foreach (DataRow row in new FinancePaymentRepository().GetAll().Rows)
                    list.Add(MapPayment(row));
            }
            catch (Exception ex)
            {
                ControllerError.Capture(this, _logger, ex, "Failed to load finance payments for {UserId}",
                    User.Identity?.Name ?? "anonymous");
            }
            return View(list);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewData["Title"] = "New Payment";
            var model = new FinanceVoucherCreateModel { PaymentDate = DateTime.Now };
            LoadVoucherLookups();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FinanceVoucherCreateModel model)
        {
            ViewData["Title"] = "New Payment";
            model.Allocations ??= new();
            model.Allocations = model.Allocations
                .Where(a => !string.IsNullOrWhiteSpace(a.OrderId) && a.AllocatedAmount > 0)
                .ToList();

            ValidateVoucher(model, isExpense: false);
            if (!ModelState.IsValid)
            {
                LoadVoucherLookups();
                return View(model);
            }

            try
            {
                var attachments = await SaveUploadsAsync(model.UploadDocuments, "payments");
                var allocJson = JsonSerializer.Serialize(model.Allocations.Select(a => new
                {
                    orderId = a.OrderId,
                    documentType = a.DocumentType,
                    totalAmount = a.TotalAmount,
                    dueAmount = a.DueAmount,
                    allocatedAmount = a.AllocatedAmount
                }));
                var attJson = JsonSerializer.Serialize(attachments);

                var result = new FinancePaymentRepository().Submit(
                    new NumberFormatStore().NextNumber("Receipt", "RCP-"),
                    model.IsSettlement,
                    model.BillingCompanyId,
                    model.PaymentDate,
                    model.PaymentModeId,
                    model.Description,
                    model.PartyId,
                    model.PaymentAccountId,
                    model.PaymentAmount,
                    model.ReferenceNo,
                    model.BusinessSegmentId,
                    model.BusinessCategoryId,
                    model.BusinessSubCategoryId,
                    User.Identity?.Name,
                    CurrentUserId(),
                    allocJson,
                    attJson);

                var status = result.Rows.Count > 0 ? result.Rows[0]["ApprovalStatus"]?.ToString() : "Approved";
                TempData["Success"] = status == "PendingApproval"
                    ? "Payment submitted and routed for approval."
                    : "Payment saved and posted to ledger.";
                return RedirectToAction(nameof(Receipts));
            }
            catch (Exception ex)
            {
                ControllerError.AddModelError(this, _logger, ex, "Failed to submit payment for {UserId}",
                    User.Identity?.Name ?? "anonymous");
                LoadVoucherLookups();
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult PartyBalance(int partyId)
        {
            try
            {
                var dt = new FinanceMasterStore().GetPartyBalance(partyId);
                if (dt.Rows.Count == 0) return Json(new { ok = true, balance = 0m, drCr = "DR", display = "₹ 0.0000 DR" });
                var row = dt.Rows[0];
                var bal = Convert.ToDecimal(row["AbsoluteBalance"]);
                var drCr = row["DrCr"]?.ToString() ?? "DR";
                return Json(new
                {
                    ok = true,
                    balance = Convert.ToDecimal(row["Balance"]),
                    absoluteBalance = bal,
                    drCr,
                    display = $"₹ {bal:N4} {drCr}"
                });
            }
            catch (Exception ex)
            {
                return Json(ControllerError.JsonError(this, _logger, ex, "PartyBalance failed for {PartyId}", partyId));
            }
        }

        [HttpGet]
        public IActionResult PartyDocuments(int partyId)
        {
            try
            {
                var items = new List<object>();
                foreach (DataRow row in new FinanceMasterStore().GetPartyOpenDocuments(partyId).Rows)
                {
                    items.Add(new
                    {
                        orderId = row["DocumentId"]?.ToString(),
                        documentType = row["DocumentType"]?.ToString(),
                        totalAmount = Convert.ToDecimal(row["TotalAmount"]),
                        dueAmount = Convert.ToDecimal(row["DueAmount"])
                    });
                }
                return Json(new { ok = true, items });
            }
            catch (Exception ex)
            {
                return Json(ControllerError.JsonError(this, _logger, ex, "PartyDocuments failed for {PartyId}", partyId));
            }
        }

        [HttpGet]
        public IActionResult SearchParties(string q)
        {
            try
            {
                var items = new List<object>();
                foreach (DataRow row in new FinanceMasterStore().SearchParties(q ?? "").Rows)
                {
                    items.Add(new
                    {
                        partyId = Convert.ToInt32(row["PartyId"]),
                        partyName = row["PartyName"]?.ToString(),
                        partyType = row["PartyType"]?.ToString(),
                        phone = row["Phone"]?.ToString()
                    });
                }
                return Json(new { ok = true, items });
            }
            catch (Exception ex)
            {
                return Json(ControllerError.JsonError(this, _logger, ex, "SearchParties failed"));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateParty([FromForm] PartyCreateModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { ok = false, error = "Party name is required." });
            try
            {
                var id = new FinanceMasterStore().InsertParty(model.PartyName.Trim(), model.PartyType, model.Phone, model.Email, model.GSTIN);
                if (id == -1) return Json(new { ok = false, error = "Party already exists." });
                if (id <= 0) return Json(new { ok = false, error = "Could not create party." });
                return Json(new { ok = true, partyId = id, partyName = model.PartyName.Trim() });
            }
            catch (Exception ex)
            {
                return Json(ControllerError.JsonError(this, _logger, ex, "CreateParty failed"));
            }
        }

        private void LoadVoucherLookups() => FinanceVoucherUi.LoadLookups(this);

        private void ValidateVoucher(FinanceVoucherCreateModel model, bool isExpense) =>
            FinanceVoucherUi.Validate(ModelState, model, isExpense);

        private async Task<List<object>> SaveUploadsAsync(List<IFormFile>? files, string folder) =>
            await FinanceVoucherUi.SaveUploadsAsync(_env, files, folder);

        private int? CurrentUserId()
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(id, out var n) ? n : null;
        }

        private static FinanceVoucherListItem MapPayment(DataRow row) => new()
        {
            Id = Convert.ToInt32(row["PaymentId"]),
            DocumentNo = row["PaymentNo"]?.ToString() ?? "",
            PaymentDate = Convert.ToDateTime(row["PaymentDate"]),
            PartyName = row["PartyName"]?.ToString() ?? "",
            ModeName = row["ModeName"]?.ToString() ?? "",
            AccountName = row["AccountName"]?.ToString() ?? "",
            BillingCompanyName = row["BillingCompanyName"]?.ToString(),
            PaymentAmount = Convert.ToDecimal(row["PaymentAmount"]),
            ApprovalStatus = row["ApprovalStatus"]?.ToString() ?? "",
            IsSettlement = row["IsSettlement"] != DBNull.Value && Convert.ToBoolean(row["IsSettlement"]),
            CreatedAt = Convert.ToDateTime(row["CreatedAt"])
        };
    }

    [Authorize]
    public class ExpenseController : Controller
    {
        private readonly ILogger<ExpenseController> _logger;
        private readonly IWebHostEnvironment _env;

        public ExpenseController(ILogger<ExpenseController> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        [HttpGet]
        public IActionResult Booking()
        {
            ViewData["Title"] = "Expense Booking";
            var list = new List<FinanceVoucherListItem>();
            try
            {
                foreach (DataRow row in new FinanceExpenseRepository().GetAll().Rows)
                {
                    list.Add(new FinanceVoucherListItem
                    {
                        Id = Convert.ToInt32(row["ExpenseId"]),
                        DocumentNo = row["ExpenseNo"]?.ToString() ?? "",
                        PaymentDate = Convert.ToDateTime(row["PaymentDate"]),
                        PartyName = row["PartyName"]?.ToString() ?? "",
                        ModeName = row["ModeName"]?.ToString() ?? "",
                        AccountName = row["AccountName"]?.ToString() ?? "",
                        BillingCompanyName = row["BillingCompanyName"]?.ToString(),
                        PaymentAmount = Convert.ToDecimal(row["PaymentAmount"]),
                        ApprovalStatus = row["ApprovalStatus"]?.ToString() ?? "",
                        IsSettlement = row["IsSettlement"] != DBNull.Value && Convert.ToBoolean(row["IsSettlement"]),
                        CreatedAt = Convert.ToDateTime(row["CreatedAt"])
                    });
                }
            }
            catch (Exception ex)
            {
                ControllerError.Capture(this, _logger, ex, "Failed to load expenses for {UserId}",
                    User.Identity?.Name ?? "anonymous");
            }
            return View(list);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewData["Title"] = "Book Expense";
            var model = new FinanceVoucherCreateModel
            {
                PaymentDate = DateTime.Now,
                ExpenseAllocations = new List<ExpenseAllocationRowModel> { new() }
            };
            ViewData["IsExpense"] = true;
            FinanceVoucherUi.LoadLookups(this);
            return View("~/Views/Payment/Create.cshtml", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FinanceVoucherCreateModel model)
        {
            ViewData["Title"] = "Book Expense";
            ViewData["IsExpense"] = true;
            model.ExpenseAllocations ??= new();
            model.ExpenseAllocations = model.ExpenseAllocations
                .Where(a => a.ExpenseHeadId > 0 && a.AllocatedAmount > 0).ToList();

            FinanceVoucherUi.Validate(ModelState, model, isExpense: true);
            if (!ModelState.IsValid)
            {
                FinanceVoucherUi.LoadLookups(this);
                return View("~/Views/Payment/Create.cshtml", model);
            }

            try
            {
                var attachments = await FinanceVoucherUi.SaveUploadsAsync(_env, model.UploadDocuments, "expenses");
                var allocJson = JsonSerializer.Serialize(model.ExpenseAllocations.Select(a => new
                {
                    expenseHeadId = a.ExpenseHeadId,
                    totalAmount = a.TotalAmount,
                    dueAmount = a.DueAmount,
                    allocatedAmount = a.AllocatedAmount
                }));
                var attJson = JsonSerializer.Serialize(attachments);

                var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var n) ? n : (int?)null;
                var result = new FinanceExpenseRepository().Submit(
                    new NumberFormatStore().NextNumber("Expense", "EXP-"),
                    model.IsSettlement,
                    model.BillingCompanyId,
                    model.PaymentDate,
                    model.PaymentModeId,
                    model.Description,
                    model.PartyId,
                    model.PaymentAccountId,
                    model.PaymentAmount,
                    model.ReferenceNo,
                    model.BusinessSegmentId,
                    model.BusinessCategoryId,
                    model.BusinessSubCategoryId,
                    User.Identity?.Name,
                    userId,
                    allocJson,
                    attJson);

                var status = result.Rows.Count > 0 ? result.Rows[0]["ApprovalStatus"]?.ToString() : "Approved";
                TempData["Success"] = status == "PendingApproval"
                    ? "Expense submitted for approval."
                    : "Expense booked and posted to ledger.";
                return RedirectToAction(nameof(Booking));
            }
            catch (Exception ex)
            {
                ControllerError.AddModelError(this, _logger, ex, "Failed to submit expense for {UserId}",
                    User.Identity?.Name ?? "anonymous");
                FinanceVoucherUi.LoadLookups(this);
                return View("~/Views/Payment/Create.cshtml", model);
            }
        }
    }

    internal static class FinanceVoucherUi
    {
        public static void LoadLookups(Controller controller)
        {
            var masters = new FinanceMasterStore();
            controller.ViewBag.PaymentModes = ToSelect(masters.GetPaymentModes(), "PaymentModeId", "ModeName");
            controller.ViewBag.PaymentAccounts = ToSelect(masters.GetPaymentAccounts(), "PaymentAccountId", "AccountName");
            controller.ViewBag.ExpenseHeads = ToSelect(masters.GetExpenseHeads(), "ExpenseHeadId", "HeadName");
            controller.ViewBag.BusinessSegments = ToSelect(masters.GetBusinessSegments(), "BusinessSegmentId", "SegmentName");
            controller.ViewBag.BusinessCategories = ToSelect(masters.GetBusinessCategories(), "BusinessCategoryId", "CategoryName");

            var companies = new List<SelectListItem> { new("-- Select billing company --", "") };
            try
            {
                foreach (DataRow row in new CompanyStore().GetAll(activeOnly: true).Rows)
                    companies.Add(new SelectListItem(row["CompanyName"]?.ToString() ?? "", row["CompanyId"]?.ToString()));
            }
            catch { /* optional */ }
            controller.ViewBag.BillingCompanies = new SelectList(companies, "Value", "Text");
        }

        public static void Validate(Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary state, FinanceVoucherCreateModel model, bool isExpense)
        {
            if (model.PaymentAmount <= 0)
                state.AddModelError(nameof(model.PaymentAmount), "Payment amount must be greater than zero.");

            var allocated = isExpense
                ? model.ExpenseAllocations?.Sum(a => a.AllocatedAmount) ?? 0
                : model.Allocations?.Sum(a => a.AllocatedAmount) ?? 0;

            if (allocated > model.PaymentAmount)
                state.AddModelError(string.Empty, "Sum of allocated amounts cannot exceed payment amount.");

            if (!isExpense)
            {
                foreach (var row in model.Allocations ?? new())
                {
                    if (row.AllocatedAmount > row.DueAmount && row.DueAmount > 0)
                        state.AddModelError(string.Empty, $"Allocated amount for {row.OrderId} exceeds due amount.");
                }
            }
        }

        public static async Task<List<object>> SaveUploadsAsync(IWebHostEnvironment env, List<IFormFile>? files, string folder)
        {
            var saved = new List<object>();
            if (files == null || files.Count == 0) return saved;

            var root = Path.Combine(env.WebRootPath ?? "wwwroot", "uploads", folder);
            Directory.CreateDirectory(root);

            foreach (var file in files.Where(f => f.Length > 0))
            {
                var safe = Path.GetFileName(file.FileName);
                var name = $"{Guid.NewGuid():N}_{safe}";
                var path = Path.Combine(root, name);
                await using var stream = System.IO.File.Create(path);
                await file.CopyToAsync(stream);
                saved.Add(new { fileName = safe, filePath = $"/uploads/{folder}/{name}" });
            }
            return saved;
        }

        private static SelectList ToSelect(DataTable dt, string value, string text)
        {
            var items = new List<SelectListItem> { new("-- Select --", "") };
            foreach (DataRow row in dt.Rows)
                items.Add(new SelectListItem(row[text]?.ToString() ?? "", row[value]?.ToString()));
            return new SelectList(items, "Value", "Text");
        }
    }

    [Authorize]
    public class ApprovalWorkflowController : Controller
    {
        private readonly ILogger<ApprovalWorkflowController> _logger;

        public ApprovalWorkflowController(ILogger<ApprovalWorkflowController> logger) => _logger = logger;

        [HttpGet]
        public IActionResult Index()
        {
            ViewData["Title"] = "Approval Workflow";
            ViewData["SettingsSection"] = "approvals";
            var list = new List<ApprovalWorkflowListItem>();
            try
            {
                foreach (DataRow row in new ApprovalWorkflowStore().GetList().Rows)
                {
                    list.Add(new ApprovalWorkflowListItem
                    {
                        Id = Convert.ToInt32(row["Id"]),
                        FormTypeName = row["FormTypeName"]?.ToString() ?? "",
                        SegmentName = row["SegmentName"]?.ToString() ?? "",
                        CategoryName = row["CategoryName"]?.ToString(),
                        BranchName = row["BranchName"]?.ToString(),
                        ApprovalLevel = Convert.ToInt32(row["ApprovalLevel"]),
                        ApprovalUserName = row["ApprovalUserName"]?.ToString(),
                        ApprovalRoleName = row["ApprovalRoleName"]?.ToString(),
                        IsReportingManager = row["IsReportingManager"] != DBNull.Value && Convert.ToBoolean(row["IsReportingManager"]),
                        ApprovalForName = row["ApprovalForName"]?.ToString() ?? ""
                    });
                }
            }
            catch (Exception ex)
            {
                ControllerError.Capture(this, _logger, ex, "Failed to load approval workflows for {UserId}",
                    User.Identity?.Name ?? "anonymous");
            }
            return View(list);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewData["Title"] = "Approval Workflow";
            ViewData["SettingsSection"] = "approvals";
            LoadApprovalLookups();
            return View(new ApprovalWorkflowEditModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ApprovalWorkflowEditModel model)
        {
            ViewData["Title"] = "Approval Workflow";
            ViewData["SettingsSection"] = "approvals";
            NormalizeApprover(model);
            if (!ValidateApproverExclusive(model))
            {
                LoadApprovalLookups();
                return View(model);
            }

            try
            {
                var id = new ApprovalWorkflowStore().Insert(
                    ApprovalWorkflowService.BuildWorkflowParams(model, User.Identity?.Name, isUpdate: false));
                if (id > 0)
                {
                    TempData["Success"] = "Approval workflow saved.";
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError(string.Empty, "Could not save workflow.");
            }
            catch (Exception ex)
            {
                ControllerError.AddModelError(this, _logger, ex, "Failed to save approval workflow for {UserId}",
                    User.Identity?.Name ?? "anonymous");
            }
            LoadApprovalLookups();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            try
            {
                new ApprovalWorkflowStore().Delete(id, User.Identity?.Name);
                TempData["Success"] = "Workflow deactivated.";
            }
            catch (Exception ex)
            {
                ControllerError.Capture(this, _logger, ex, "Failed to delete approval workflow {Id}", id);
            }
            return RedirectToAction(nameof(Index));
        }

        private void LoadApprovalLookups()
        {
            var m = new FinanceMasterStore();
            ViewBag.FormTypes = ToSelect(m.GetFormTypes(), "FormTypeId", "FormTypeName");
            ViewBag.Segments = ToSelect(m.GetBusinessSegments(), "BusinessSegmentId", "SegmentName");
            ViewBag.Categories = ToSelect(m.GetBusinessCategories(), "BusinessCategoryId", "CategoryName");
            ViewBag.SubCategories = ToSelect(m.GetBusinessSubCategories(), "BusinessSubCategoryId", "SubCategoryName");
            ViewBag.ApprovalFor = ToSelect(m.GetApprovalForLookup(), "ApprovalForId", "ApprovalForName");
            ViewBag.EmailTemplates = ToSelect(m.GetNotificationTemplates("Email"), "TemplateId", "TemplateName");
            ViewBag.SmsTemplates = ToSelect(m.GetNotificationTemplates("SMS"), "TemplateId", "TemplateName");

            var companies = new List<SelectListItem> { new("-- Select branch / company --", "") };
            try
            {
                foreach (DataRow row in new CompanyStore().GetAll(activeOnly: true).Rows)
                    companies.Add(new SelectListItem(row["CompanyName"]?.ToString() ?? "", row["CompanyId"]?.ToString()));
            }
            catch { }
            ViewBag.Branches = new SelectList(companies, "Value", "Text");

            var users = new List<SelectListItem> { new("-- Select user --", "") };
            var roles = new List<SelectListItem> { new("-- Select role --", "") };
            try
            {
                var admin = new UserAdmin();
                foreach (DataRow row in admin.GetAllUsers().Rows)
                {
                    var name = $"{row["FirstName"]} {row["LastName"]}".Trim();
                    users.Add(new SelectListItem(name, row["User_Id"]?.ToString()));
                }
                foreach (DataRow row in admin.GetAllRoles().Rows)
                    roles.Add(new SelectListItem(row["Role_Name"]?.ToString() ?? "", row["Role_Id"]?.ToString()));
            }
            catch { }
            ViewBag.Users = new SelectList(users, "Value", "Text");
            ViewBag.Roles = new SelectList(roles, "Value", "Text");
        }

        private void NormalizeApprover(ApprovalWorkflowEditModel model)
        {
            model.IsReportingManager = string.Equals(model.ApproverKind, "reportingManager", StringComparison.OrdinalIgnoreCase);
            if (model.IsReportingManager)
            {
                model.ApprovalUserId = null;
                model.ApprovalRoleId = null;
            }
            else if (string.Equals(model.ApproverKind, "role", StringComparison.OrdinalIgnoreCase))
            {
                model.ApprovalUserId = null;
                model.IsReportingManager = false;
            }
            else
            {
                model.ApprovalRoleId = null;
                model.IsReportingManager = false;
            }
        }

        private bool ValidateApproverExclusive(ApprovalWorkflowEditModel model)
        {
            var flags = (model.ApprovalUserId.HasValue ? 1 : 0)
                        + (model.ApprovalRoleId.HasValue ? 1 : 0)
                        + (model.IsReportingManager ? 1 : 0);
            if (flags != 1)
            {
                ModelState.AddModelError(string.Empty, "Select exactly one of Approval User, Approval Role, or Reporting Manager.");
                return false;
            }
            return ModelState.IsValid;
        }

        private static SelectList ToSelect(DataTable dt, string value, string text)
        {
            var items = new List<SelectListItem> { new("-- Select --", "") };
            foreach (DataRow row in dt.Rows)
                items.Add(new SelectListItem(row[text]?.ToString() ?? "", row[value]?.ToString()));
            return new SelectList(items, "Value", "Text");
        }
    }

    [Authorize]
    public class ApprovalController : Controller
    {
        private readonly ILogger<ApprovalController> _logger;

        public ApprovalController(ILogger<ApprovalController> logger) => _logger = logger;

        [HttpGet]
        public IActionResult Inbox()
        {
            ViewData["Title"] = "Approval Inbox";
            var list = new List<ApprovalInboxItem>();
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userId, out var uid))
                {
                    TempData["Error"] = "User id claim missing — sign in again.";
                    return View(list);
                }

                foreach (DataRow row in new ApprovalWorkflowStore().GetPendingForUser(uid).Rows)
                {
                    list.Add(new ApprovalInboxItem
                    {
                        Id = Convert.ToInt32(row["Id"]),
                        FormTypeKey = row["FormTypeKey"]?.ToString() ?? "",
                        FormTypeName = row["FormTypeName"]?.ToString() ?? "",
                        RecordId = Convert.ToInt32(row["RecordId"]),
                        ApprovalLevel = Convert.ToInt32(row["ApprovalLevel"]),
                        RequestedAt = Convert.ToDateTime(row["RequestedAt"]),
                        RequestedByName = row["RequestedByName"]?.ToString(),
                        Remarks = row["Remarks"]?.ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                ControllerError.Capture(this, _logger, ex, "Failed to load approval inbox for {UserId}",
                    User.Identity?.Name ?? "anonymous");
            }
            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Approve(ApprovalActionModel model)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                new ApprovalWorkflowStore().Approve(model.ApprovalRequestId, userId, model.Remarks);
                TempData["Success"] = "Approved.";
            }
            catch (Exception ex)
            {
                ControllerError.Capture(this, _logger, ex, "Approve failed for request {RequestId}", model.ApprovalRequestId);
            }
            return RedirectToAction(nameof(Inbox));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reject(ApprovalActionModel model)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                new ApprovalWorkflowStore().Reject(model.ApprovalRequestId, userId, model.Remarks);
                TempData["Success"] = "Rejected — record will not post to ledger.";
            }
            catch (Exception ex)
            {
                ControllerError.Capture(this, _logger, ex, "Reject failed for request {RequestId}", model.ApprovalRequestId);
            }
            return RedirectToAction(nameof(Inbox));
        }
    }
}
