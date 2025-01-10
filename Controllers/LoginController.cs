using DataLayer;
using Inventory.Models;
using Microsoft.AspNetCore.Mvc;
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

        // GET: LoginController
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginModel model)
        {
            try
            {
                // Create a new Login object and set its properties
                Login login = new Login
                {
                    Email = model.Email,
                    Password = EncodePassword(model.Password)  // Encode password before storing or verifying
                };

                // Check if the user exists and log them in
                if (login.LoginUser())
                {
                    return RedirectToAction("Index", "SignUp");
                }
                else
                {
                    _logger.LogWarning("Login failed for user: " + model.Email);
                    ViewBag.Message = "INVALID EMAIL OR PASSWORD.";
                    return View();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while trying to log in.");
                ViewBag.Message = "An error occurred. Please try again later.";
                return View();
            }
        }

        // Method to encode a string using SHA-256
        private string EncodePassword(string password)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));

                // Convert byte array to a string
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
