using DataLayer;
using Inventory.Infrastructure;
using Inventory.Models;
using Inventory.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace Inventory.Controllers
{
    [Authorize]
    public class SettingsController : Controller
    {
        private readonly ILogger<SettingsController> _logger;

        public SettingsController(ILogger<SettingsController> logger)
        {
            _logger = logger;
        }

        private IActionResult? RequireSettingsAccess()
        {
            if (ModuleAccess.CanAccess(User, "Settings")) return null;
            TempData["Error"] = "You do not have access to Settings. Ask an Admin to grant the Settings module on your role.";
            return RedirectToAction("Index", "Dashboard");
        }

        [HttpGet]
        public IActionResult Index()
        {
            var denied = RequireSettingsAccess();
            if (denied != null) return denied;
            ViewData["Title"] = "Settings";
            return View();
        }

        [HttpGet]
        public IActionResult Users()
        {
            var denied = RequireSettingsAccess();
            if (denied != null) return denied;
            ViewData["Title"] = "User Setup";
            return View(LoadPage("users"));
        }

        [HttpGet]
        public IActionResult Roles()
        {
            var denied = RequireSettingsAccess();
            if (denied != null) return denied;
            ViewData["Title"] = "Role Setup";
            return View(LoadPage("roles"));
        }

        [HttpGet]
        public IActionResult Rights()
        {
            var denied = RequireSettingsAccess();
            if (denied != null) return denied;
            ViewData["Title"] = "Module Rights";
            return View(LoadPage("rights"));
        }

        [HttpGet]
        public IActionResult Companies()
        {
            var denied = RequireSettingsAccess();
            if (denied != null) return denied;
            ViewData["Title"] = "Companies";
            return View(LoadPage("companies"));
        }

        [HttpGet]
        public IActionResult Prefixes()
        {
            var denied = RequireSettingsAccess();
            if (denied != null) return denied;
            ViewData["Title"] = "Number Formats";
            return View(LoadPage("prefixes"));
        }

        [HttpGet]
        public IActionResult Lookups(string tab = "values", int? categoryId = null)
        {
            var denied = RequireSettingsAccess();
            if (denied != null) return denied;
            ViewData["Title"] = "Lookups";
            var model = LoadPage("lookups");
            model.Lookups.ActiveTab = string.IsNullOrWhiteSpace(tab) ? "values" : tab;
            model.Lookups.SelectedCategoryId = categoryId;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateUser(CreateUserModel model)
        {
            var denied = RequireSettingsAccess();
            if (denied != null) return denied;

            if (!ModelState.IsValid)
            {
                var page = LoadPage("users");
                page.NewUser = model;
                return View("Users", page);
            }

            try
            {
                var result = new UserAdmin
                {
                    FirstName = model.FirstName.Trim(),
                    LastName = model.LastName.Trim(),
                    Email = model.Email.Trim().ToLowerInvariant(),
                    Password = Sha256(model.Password),
                    Role_Id = model.Role_Id,
                    Phone = model.Phone,
                    Status = model.Status ?? "Active"
                }.InsertUser();

                if (result == -1)
                {
                    ModelState.AddModelError(nameof(model.Email), "A user with this email already exists.");
                    var page = LoadPage("users");
                    page.NewUser = model;
                    return View("Users", page);
                }

                if (result > 0)
                {
                    TempData["Success"] = $"User {model.Email} created.";
                    return RedirectToAction(nameof(Users));
                }

                ModelState.AddModelError(string.Empty, "Could not create user.");
            }
            catch (Exception ex)
            {
                ControllerError.Capture(this, _logger, ex,
                    "Create user failed for email {Email}",
                    model.Email);
            }

            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AssignRole(int userId, int roleId)
        {
            var denied = RequireSettingsAccess();
            if (denied != null) return denied;

            try
            {
                if (new UserAdmin().UpdateRole(userId, roleId) > 0)
                    TempData["Success"] = "Role updated.";
                else
                    TempData["Error"] = "Role update failed.";
            }
            catch (Exception ex)
            {
                ControllerError.Capture(this, _logger, ex,
                    "Assign role failed for user {UserId} to role {RoleId}",
                    userId, roleId);
            }
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetStatus(int userId, string status)
        {
            var denied = RequireSettingsAccess();
            if (denied != null) return denied;

            var next = string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase) ? "Inactive" : "Active";
            try
            {
                if (new UserAdmin().UpdateStatus(userId, next) > 0)
                    TempData["Success"] = $"User marked {next}.";
                else
                    TempData["Error"] = "Status update failed.";
            }
            catch (Exception ex)
            {
                ControllerError.Capture(this, _logger, ex,
                    "Status update failed for user {UserId} to {Status}",
                    userId, next);
            }
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteUser(int userId)
        {
            var denied = RequireSettingsAccess();
            if (denied != null) return denied;

            var selfId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (selfId == userId.ToString())
            {
                TempData["Error"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(Users));
            }

            try
            {
                TempData[new UserAdmin().Delete(userId) > 0 ? "Success" : "Error"] =
                    "User deleted.";
            }
            catch (Exception ex)
            {
                ControllerError.Capture(this, _logger, ex,
                    "Delete user failed for user {UserId}",
                    userId);
            }
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateRole(RoleEditModel model)
        {
            var denied = RequireSettingsAccess();
            if (denied != null) return denied;

            if (!ModelState.IsValid)
            {
                var page = LoadPage("roles");
                page.NewRole = model;
                return View("Roles", page);
            }

            try
            {
                var result = new UserAdmin().InsertRole(model.Role_Name.Trim(), model.Description);
                if (result == -1)
                {
                    ModelState.AddModelError(nameof(model.Role_Name), "A role with this name already exists.");
                    var page = LoadPage("roles");
                    page.NewRole = model;
                    return View("Roles", page);
                }

                if (result > 0)
                {
                    TempData["Success"] = $"Role “{model.Role_Name}” created. Assign module rights next.";
                    return RedirectToAction(nameof(Rights));
                }

                TempData["Error"] = "Could not create role.";
            }
            catch (Exception ex)
            {
                ControllerError.Capture(this, _logger, ex,
                    "CreateRole failed for {RoleName}",
                    model.Role_Name);
            }

            return RedirectToAction(nameof(Roles));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteRole(int roleId)
        {
            var denied = RequireSettingsAccess();
            if (denied != null) return denied;

            try
            {
                var result = new UserAdmin().DeleteRole(roleId);
                TempData[result > 0 ? "Success" : "Error"] = result switch
                {
                    -2 => "System roles (Admin / Manager / Staff) cannot be deleted.",
                    -3 => "Role is assigned to users. Reassign them first.",
                    > 0 => "Role deactivated.",
                    _ => "Could not delete role."
                };
            }
            catch (Exception ex)
            {
                ControllerError.Capture(this, _logger, ex,
                    "DeleteRole failed for role {RoleId}",
                    roleId);
            }

            return RedirectToAction(nameof(Roles));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveRoleModules(int roleId, string[] moduleKeys)
        {
            var denied = RequireSettingsAccess();
            if (denied != null) return denied;

            try
            {
                var result = new UserAdmin().SaveRoleModules(roleId, moduleKeys ?? Array.Empty<string>());
                TempData[result > 0 ? "Success" : "Error"] = result > 0
                    ? "Module rights saved. Affected users must sign in again."
                    : "Could not save module rights.";
            }
            catch (Exception ex)
            {
                ControllerError.Capture(this, _logger, ex,
                    "Save role modules failed for role {RoleId}",
                    roleId);
            }

            return RedirectToAction(nameof(Rights));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SavePrefixes(List<NumberFormatRow> formats)
        {
            var denied = RequireSettingsAccess();
            if (denied != null) return denied;

            try
            {
                var store = new NumberFormatStore();
                var saved = 0;
                foreach (var row in formats ?? new List<NumberFormatRow>())
                {
                    if (row.Format_Id <= 0 || string.IsNullOrWhiteSpace(row.Prefix)) continue;
                    if (store.Update(
                            row.Format_Id,
                            row.Location_Code,
                            row.Business_Segment,
                            row.Business_Category,
                            row.Business_SubCategory,
                            row.Prefix.Trim(),
                            row.Starting_Number) > 0)
                        saved++;
                }

                TempData[saved > 0 ? "Success" : "Error"] = saved > 0
                    ? "Number formats saved."
                    : "Could not save number formats.";
            }
            catch (Exception ex)
            {
                ControllerError.Capture(this, _logger, ex,
                    "Save number formats failed for {UserId}",
                    User.Identity?.Name ?? "anonymous");
            }

            return RedirectToAction(nameof(Prefixes));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateCompany(CompanyEditModel model)
        {
            var denied = RequireSettingsAccess();
            if (denied != null) return denied;

            if (!ModelState.IsValid)
            {
                var page = LoadPage("companies");
                page.NewCompany = model;
                return View("Companies", page);
            }

            try
            {
                var result = new CompanyStore
                {
                    CompanyName = model.CompanyName.Trim(),
                    Gstin = model.Gstin,
                    Email = model.Email,
                    Phone = model.Phone,
                    AddressLine = model.AddressLine
                }.Insert();

                if (result == -1)
                {
                    ModelState.AddModelError(nameof(model.CompanyName), "A company with this name already exists.");
                    var page = LoadPage("companies");
                    page.NewCompany = model;
                    return View("Companies", page);
                }

                if (result > 0)
                {
                    TempData["Success"] = $"Company “{model.CompanyName}” added.";
                    return RedirectToAction(nameof(Companies));
                }

                ModelState.AddModelError(string.Empty, "Could not save company.");
            }
            catch (Exception ex)
            {
                ControllerError.AddModelError(this, _logger, ex,
                    "CreateCompany failed for {CompanyName}",
                    model.CompanyName);
            }

            var fail = LoadPage("companies");
            fail.NewCompany = model;
            return View("Companies", fail);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeactivateCompany(Guid id)
        {
            var denied = RequireSettingsAccess();
            if (denied != null) return denied;

            try
            {
                new CompanyStore().Deactivate(id);
                TempData["Success"] = "Company deactivated.";
            }
            catch (Exception ex)
            {
                ControllerError.Capture(this, _logger, ex,
                    "DeactivateCompany failed for company {CompanyId}",
                    id);
            }

            return RedirectToAction(nameof(Companies));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateLookupCategory(LookupCategoryEditModel model)
        {
            var denied = RequireSettingsAccess();
            if (denied != null) return denied;

            try
            {
                var key = model.Category_Key.Trim().Replace(" ", "");
                var result = new LookupAdmin().InsertCategory(key, model.Category_Name.Trim(), model.Description);
                TempData[result > 0 ? "Success" : "Error"] = result == -1
                    ? "Category key already exists."
                    : result > 0 ? "Lookup category added." : "Could not add category.";
            }
            catch (Exception ex)
            {
                ControllerError.Capture(this, _logger, ex,
                    "CreateLookupCategory failed for {CategoryKey}",
                    model.Category_Key);
            }

            return RedirectToAction(nameof(Lookups), new { tab = "values" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateLookupItem(LookupValueEditModel model)
        {
            var denied = RequireSettingsAccess();
            if (denied != null) return denied;

            try
            {
                var result = new LookupAdmin().InsertItem(model.Category_Id, model.Item_Code, model.Item_Label.Trim(), model.Sort_Order);
                TempData[result > 0 ? "Success" : "Error"] = result > 0 ? "Lookup value added." : "Could not add value.";
            }
            catch (Exception ex)
            {
                ControllerError.Capture(this, _logger, ex,
                    "CreateLookupItem failed for category {CategoryId}",
                    model.Category_Id);
            }

            return RedirectToAction(nameof(Lookups), new { tab = "values", categoryId = model.Category_Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeactivateLookupItem(int id, int? categoryId)
        {
            var denied = RequireSettingsAccess();
            if (denied != null) return denied;

            try
            {
                new LookupAdmin().DeactivateItem(id);
                TempData["Success"] = "Lookup value deactivated.";
            }
            catch (Exception ex)
            {
                ControllerError.Capture(this, _logger, ex,
                    "DeactivateLookupItem failed for item {ItemId}",
                    id);
            }

            return RedirectToAction(nameof(Lookups), new { tab = "values", categoryId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateState(LookupStateEditModel model)
        {
            var denied = RequireSettingsAccess();
            if (denied != null) return denied;

            try
            {
                var result = new LookupAdmin().InsertState(model.State_Name.Trim(), model.State_Code);
                TempData[result > 0 ? "Success" : "Error"] = result == -1
                    ? "State already exists."
                    : result > 0 ? "State added." : "Could not add state.";
            }
            catch (Exception ex)
            {
                ControllerError.Capture(this, _logger, ex,
                    "CreateState failed for {StateName}",
                    model.State_Name);
            }

            return RedirectToAction(nameof(Lookups), new { tab = "location" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateCity(LookupCityEditModel model)
        {
            var denied = RequireSettingsAccess();
            if (denied != null) return denied;

            try
            {
                var result = new LookupAdmin().InsertCity(model.State_Id, model.City_Name.Trim());
                TempData[result > 0 ? "Success" : "Error"] = result == -1
                    ? "City already exists for this state."
                    : result > 0 ? "City added." : "Could not add city.";
            }
            catch (Exception ex)
            {
                ControllerError.Capture(this, _logger, ex,
                    "CreateCity failed for {CityName} in state {StateId}",
                    model.City_Name, model.State_Id);
            }

            return RedirectToAction(nameof(Lookups), new { tab = "location" });
        }

        private SettingsPageModel LoadPage(string section)
        {
            var model = new SettingsPageModel { ActiveSection = section };
            try
            {
                var admin = new UserAdmin();
                foreach (DataRow row in admin.GetAllRoles().Rows)
                {
                    model.Roles.Add(new RoleItem
                    {
                        Role_Id = Convert.ToInt32(row["Role_Id"]),
                        Role_Name = row["Role_Name"]?.ToString(),
                        Description = row["Description"]?.ToString(),
                        Is_System = row.Table.Columns.Contains("Is_System")
                            && row["Is_System"] != DBNull.Value
                            && Convert.ToBoolean(row["Is_System"])
                    });
                }

                foreach (DataRow row in admin.GetAllModules().Rows)
                {
                    model.Modules.Add(new ModuleItem
                    {
                        Module_Id = Convert.ToInt32(row["Module_Id"]),
                        Module_Key = row["Module_Key"]?.ToString() ?? "",
                        Module_Name = row["Module_Name"]?.ToString() ?? "",
                        Description = row["Description"]?.ToString(),
                        Module_Group = row["Module_Group"]?.ToString() ?? "",
                        Icon_Class = row["Icon_Class"]?.ToString(),
                        Display_Order = Convert.ToInt32(row["Display_Order"])
                    });
                }

                foreach (DataRow row in admin.GetRoleModuleMatrix().Rows)
                {
                    var roleId = Convert.ToInt32(row["Role_Id"]);
                    var role = model.Roles.FirstOrDefault(r => r.Role_Id == roleId);
                    var moduleKey = row["Module_Key"]?.ToString();
                    if (role != null && !string.IsNullOrWhiteSpace(moduleKey))
                        role.ModuleKeys.Add(moduleKey);
                }

                if (section is "users" or "roles" or "rights" or "hub")
                {
                    foreach (DataRow row in admin.GetAllUsers().Rows)
                    {
                        model.Users.Add(new UserListItem
                        {
                            User_Id = Convert.ToInt32(row["User_Id"]),
                            FirstName = row["FirstName"]?.ToString(),
                            LastName = row["LastName"]?.ToString(),
                            Email = row["Email"]?.ToString(),
                            Role_Id = row["Role_Id"] != DBNull.Value ? Convert.ToInt32(row["Role_Id"]) : null,
                            Role_Name = row.Table.Columns.Contains("Role_Name") ? row["Role_Name"]?.ToString() : null,
                            Status = row.Table.Columns.Contains("Status") ? row["Status"]?.ToString() : "Active",
                            Phone = row.Table.Columns.Contains("Phone") ? row["Phone"]?.ToString() : null,
                            Created_Date = row["Created_Date"] != DBNull.Value
                                ? Convert.ToDateTime(row["Created_Date"]) : DateTime.MinValue
                        });
                    }
                }

                if (section == "prefixes")
                {
                    try
                    {
                        foreach (DataRow row in new NumberFormatStore().GetAll().Rows)
                        {
                            model.NumberFormats.Add(new NumberFormatRow
                            {
                                Format_Id = Convert.ToInt32(row["Format_Id"]),
                                Form_Key = row["Form_Key"]?.ToString() ?? "",
                                Form_Name = row["Form_Name"]?.ToString() ?? "",
                                Location_Code = row["Location_Code"]?.ToString(),
                                Business_Segment = row["Business_Segment"]?.ToString(),
                                Business_Category = row["Business_Category"]?.ToString(),
                                Business_SubCategory = row["Business_SubCategory"]?.ToString(),
                                Prefix = row["Prefix"]?.ToString() ?? "",
                                Starting_Number = row["Starting_Number"] != DBNull.Value ? Convert.ToInt32(row["Starting_Number"]) : 1,
                                Next_Number = row["Next_Number"] != DBNull.Value ? Convert.ToInt32(row["Next_Number"]) : 1
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        ControllerError.CaptureWarning(this, _logger, ex,
                            "Number formats load failed for {UserId}",
                            User.Identity?.Name ?? "anonymous");
                    }
                }

                if (section == "companies")
                {
                    try
                    {
                        foreach (DataRow row in new CompanyStore().GetAll().Rows)
                        {
                            model.Companies.Add(new CompanyListItem
                            {
                                CompanyId = (Guid)row["CompanyId"],
                                CompanyName = row["CompanyName"]?.ToString() ?? "",
                                Gstin = row["Gstin"]?.ToString(),
                                Email = row["Email"]?.ToString(),
                                Phone = row["Phone"]?.ToString(),
                                AddressLine = row["AddressLine"]?.ToString(),
                                IsActive = row["IsActive"] != DBNull.Value && Convert.ToBoolean(row["IsActive"]),
                                CreatedAt = row["CreatedAt"] != DBNull.Value
                                    ? Convert.ToDateTime(row["CreatedAt"]) : DateTime.MinValue
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        ControllerError.CaptureWarning(this, _logger, ex,
                            "Companies load failed for {UserId}",
                            User.Identity?.Name ?? "anonymous");
                    }
                }

                if (section == "lookups")
                    LoadLookups(model);

                var staff = model.Roles.FirstOrDefault(r => r.Role_Name == "Staff");
                model.NewUser.Role_Id = staff?.Role_Id ?? model.Roles.FirstOrDefault()?.Role_Id ?? 0;
            }
            catch (Exception ex)
            {
                ControllerError.Capture(this, _logger, ex,
                    "Settings load failed for section {Section} by {UserId}",
                    section, User.Identity?.Name ?? "anonymous");
            }

            ViewBag.RoleOptions = new SelectList(model.Roles, "Role_Id", "Role_Name");
            ViewBag.LookupCategoryOptions = new SelectList(model.Lookups.Categories, "Category_Id", "Category_Name");
            ViewBag.StateOptions = new SelectList(model.Lookups.States, "State_Id", "State_Name");
            return model;
        }

        private void LoadLookups(SettingsPageModel model)
        {
            try
            {
                var admin = new LookupAdmin();
                foreach (DataRow row in admin.GetCategories().Rows)
                {
                    model.Lookups.Categories.Add(new LookupCategoryItem
                    {
                        Category_Id = Convert.ToInt32(row["Category_Id"]),
                        Category_Key = row["Category_Key"]?.ToString() ?? "",
                        Category_Name = row["Category_Name"]?.ToString() ?? "",
                        Description = row["Description"]?.ToString()
                    });
                }

                foreach (DataRow row in admin.GetItems(activeOnly: true).Rows)
                {
                    model.Lookups.Items.Add(new LookupValueItem
                    {
                        Item_Id = Convert.ToInt32(row["Item_Id"]),
                        Category_Id = Convert.ToInt32(row["Category_Id"]),
                        Category_Key = row["Category_Key"]?.ToString() ?? "",
                        Category_Name = row["Category_Name"]?.ToString() ?? "",
                        Item_Code = row["Item_Code"]?.ToString(),
                        Item_Label = row["Item_Label"]?.ToString() ?? "",
                        Sort_Order = row["Sort_Order"] != DBNull.Value ? Convert.ToInt32(row["Sort_Order"]) : 0,
                        Is_Active = true
                    });
                }

                foreach (DataRow row in new Lookup().GetStates().Rows)
                {
                    model.Lookups.States.Add(new LookupStateItem
                    {
                        State_Id = Convert.ToInt32(row["State_Id"]),
                        State_Name = row["State_Name"]?.ToString() ?? "",
                        State_Code = row.Table.Columns.Contains("State_Code") ? row["State_Code"]?.ToString() : null
                    });
                }

                foreach (DataRow row in new Lookup().GetAllCities().Rows)
                {
                    model.Lookups.Cities.Add(new LookupCityItem
                    {
                        City_Id = Convert.ToInt32(row["City_Id"]),
                        State_Id = Convert.ToInt32(row["State_Id"]),
                        City_Name = row["City_Name"]?.ToString() ?? "",
                        State_Name = row.Table.Columns.Contains("State_Name") ? row["State_Name"]?.ToString() : null
                    });
                }
            }
            catch (Exception ex)
            {
                ControllerError.CaptureWarning(this, _logger, ex,
                    "Lookups load failed for {UserId}",
                    User.Identity?.Name ?? "anonymous");
            }
        }

        private static string Sha256(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
