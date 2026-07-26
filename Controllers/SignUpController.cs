using DataLayer;
using Inventory.Infrastructure;
using Inventory.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace Inventory.Controllers
{
    [AllowAnonymous]
    public class SignUpController : Controller
    {
        private readonly ILogger<SignUpController> _logger;

        public SignUpController(ILogger<SignUpController> logger)
        {
            _logger = logger;
        }

        public ActionResult SignUp()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SignUp(SignUpModel signUpModel)
        {
            if (!ModelState.IsValid)
                return View(signUpModel);

            try
            {
                var normalizedEmail = signUpModel.Email.Trim().ToLowerInvariant();
                var hashedPassword = GetSha256Hash(signUpModel.Password);

                var signup = new SignUp
                {
                    FirstName = signUpModel.FirstName.Trim(),
                    LastName = signUpModel.LastName.Trim(),
                    Email = normalizedEmail,
                    Password = hashedPassword,
                    ConfirmPassword = hashedPassword
                };

                var result = signup.InsertSIGNUP();
                if (result == -1)
                {
                    ModelState.AddModelError(nameof(signUpModel.Email),
                        "An account already exists for this email.");
                    return View(signUpModel);
                }

                if (result > 0)
                {
                    TempData["SuccessMessage"] = "Account created. You can now sign in.";
                    return RedirectToAction("Login", "Login");
                }

                ModelState.AddModelError(string.Empty,
                    "Registration could not be completed. Please try again.");
            }
            catch (Exception ex)
            {
                ControllerError.AddModelError(this, _logger, ex,
                    "Registration failed for {Email}",
                    signUpModel.Email);
            }

            return View(signUpModel);
        }


        public IActionResult Index()
        {
            return RedirectToAction("Index", "Dashboard");
        }

        private static string GetSha256Hash(string input)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
      
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

              
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
