using Inventory.Infrastructure;
using Inventory.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Inventory.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHostEnvironment _env;

        public HomeController(ILogger<HomeController> logger, IHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        public IActionResult Index() => View();

        public IActionResult Privacy() => View();

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(string? cid = null)
        {
            var correlationId = cid
                ?? HttpContext.Items["UnhandledCorrelationId"] as string
                ?? CorrelationId.Get(HttpContext);

            var feature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            var ex = HttpContext.Items["UnhandledException"] as Exception
                ?? feature?.Error;

            if (ex != null)
            {
                _logger.LogError(
                    ex,
                    "Error page rendered for {UserId} at {UtcNow}. CorrelationId={CorrelationId} Path={Path}",
                    User?.Identity?.Name ?? "anonymous",
                    DateTime.UtcNow,
                    correlationId,
                    feature?.Path ?? HttpContext.Request.Path.Value);
            }

            var model = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                CorrelationId = correlationId,
                ShowDetails = _env.IsDevelopment(),
                ErrorMessage = _env.IsDevelopment() && ex != null ? ex.GetBaseException().Message : null,
                DetailedError = _env.IsDevelopment() && ex != null ? ExceptionFormatter.Format(ex) : null
            };

            return View(model);
        }
    }
}
