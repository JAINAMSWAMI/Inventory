using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Inventory.Infrastructure
{
    /// <summary>
    /// When a POST returns the form view with ModelState errors (missing required fields),
    /// show the same ERP-style Warning toast: "Please Fill Required Detail."
    /// Skips auth controllers and when an Error/Success TempData is already set.
    /// </summary>
    public sealed class RequiredDetailWarningFilter : IActionFilter
    {
        private static readonly HashSet<string> SkipControllers = new(StringComparer.OrdinalIgnoreCase)
        {
            "Login",
            "SignUp",
            "Account"
        };

        public void OnActionExecuting(ActionExecutingContext context) { }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Controller is not Controller controller)
                return;

            var name = context.RouteData.Values["controller"]?.ToString();
            if (string.IsNullOrEmpty(name) || SkipControllers.Contains(name))
                return;

            if (context.HttpContext.Request.Method != "POST" &&
                context.HttpContext.Request.Method != "PUT")
                return;

            if (context.Result is not ViewResult)
                return;

            if (controller.ModelState.IsValid)
                return;

            if (!string.IsNullOrEmpty(controller.TempData["Error"] as string) ||
                !string.IsNullOrEmpty(controller.TempData["Success"] as string) ||
                !string.IsNullOrEmpty(controller.TempData["Warning"] as string))
                return;

            controller.TempData["Warning"] = "Please Fill Required Detail.";
        }
    }
}
