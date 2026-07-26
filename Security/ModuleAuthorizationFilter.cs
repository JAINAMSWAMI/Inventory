using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Inventory.Security
{
    public static class ModuleAccess
    {
        public const string ClaimType = "inventory_module";

        public static bool CanAccess(System.Security.Claims.ClaimsPrincipal user, string moduleKey) =>
            string.Equals(moduleKey, "Dashboard", StringComparison.OrdinalIgnoreCase) ||
            user.IsInRole("Admin") ||
            user.Claims.Any(c => c.Type == ClaimType &&
                                 string.Equals(c.Value, moduleKey, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Enforces the module selected for the current MVC controller.
    /// Sidebar visibility is only presentation; this filter is the security boundary.
    /// </summary>
    public sealed class ModuleAuthorizationFilter : IAuthorizationFilter
    {
        private static readonly IReadOnlyDictionary<string, string> ControllerModules =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Dashboard"] = "Dashboard",
                ["InventoryControl"] = "Products",
                ["Category"] = "Categories",
                ["MasterPrices"] = "Pricing",
                ["Supplier"] = "Suppliers",
                ["Warehouse"] = "Warehouses",
                ["Documents"] = "Documents",
                ["StockReceive"] = "Stock",
                ["Distribution"] = "Orders",
                ["Bom"] = "Orders",
                ["Invoice"] = "Invoices",
                ["Payment"] = "Payments",
                ["Expense"] = "Expenses",
                ["Approval"] = "Approvals",
                ["ApprovalWorkflow"] = "Settings",
                ["Reports"] = "Reports",
                ["Settings"] = "Settings"
            };

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            if (user.Identity?.IsAuthenticated != true)
                return;

            var controller = context.RouteData.Values["controller"]?.ToString();
            if (string.IsNullOrWhiteSpace(controller) ||
                !ControllerModules.TryGetValue(controller, out var moduleKey))
                return;

            if (ModuleAccess.CanAccess(user, moduleKey))
                return;

            context.Result = new RedirectToActionResult("Index", "Dashboard", null);
        }
    }
}
