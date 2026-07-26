using DataLayer;
using Inventory.Infrastructure;
using Inventory.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Inventory.Security;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Inventory.Controllers
{
    public class LoginController : Controller
    {
        private readonly ILogger<LoginController> _logger;

        public LoginController(ILogger<LoginController> logger)
        {
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Dashboard");
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var normalizedEmail = model.Email.Trim().ToLowerInvariant();
                var login = new Login
                {
                    Email = normalizedEmail,
                    Password = EncodePassword(model.Password)
                };

                if (login.LoginUser())
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, login.DisplayName),
                        new Claim(ClaimTypes.Email, login.Email ?? normalizedEmail),
                        new Claim(ClaimTypes.NameIdentifier, login.User_Id.ToString())
                    };

                    var role = string.IsNullOrWhiteSpace(login.Role_Name) ? "Staff" : login.Role_Name;
                    claims.Add(new Claim(ClaimTypes.Role, role));

                    foreach (System.Data.DataRow row in new UserAdmin().GetUserModules(login.User_Id).Rows)
                    {
                        var moduleKey = row["Module_Key"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(moduleKey))
                            claims.Add(new Claim(ModuleAccess.ClaimType, moduleKey));
                    }

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        principal,
                        new AuthenticationProperties
                        {
                            IsPersistent = model.RememberMe,
                            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                        });

                    return RedirectToAction("Index", "Dashboard");
                }

                _logger.LogWarning("Login failed for user: {Email}", model.Email);
                ViewBag.Message = "Invalid email or password.";
                return View(model);
            }
            catch (Exception ex)
            {
                ControllerError.Capture(this, _logger, ex,
                    "Login failed for {Email}",
                    model.Email);
                ViewBag.Message = TempData["Error"];
                return View(model);
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        [AllowAnonymous]
        public IActionResult Index() => RedirectToAction(nameof(Login));

        private static string EncodePassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            var builder = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
                builder.Append(b.ToString("x2"));
            return builder.ToString();
        }
    }
}
