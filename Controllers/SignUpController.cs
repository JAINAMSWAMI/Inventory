using DataLayer;
using Inventory.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Inventory.Controllers
{
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
        public IActionResult SignUp(SignUpModel signUpModel)
        {
            if (ModelState.IsValid)
            {
                if (signUpModel.Password != signUpModel.ConfirmPassword)
                {
                    ModelState.AddModelError("ConfirmPassword", "Password and confirm password do not match.");
                    return View(signUpModel);
                }

                // Hash the password using SHA-256
                string hashedPassword = GetSha256Hash(signUpModel.Password);

                // Create a new SignUp object and set its properties
                SignUp signup = new SignUp
                {
                    FirstName = signUpModel.FirstName,
                    LastName = signUpModel.LastName,
                    Email = signUpModel.Email,
                    Password = hashedPassword,
                    ConfirmPassword = hashedPassword
                };

                // Call the InsertSignUp method to insert the data into the signup table
                int result = signup.InsertSIGNUP();

                if (result > 0)
                {
                    _logger.LogInformation("User signed up successfully.");
                    return RedirectToAction("Index", "SignUp"); // Replace with appropriate redirect action
                }
                else
                {
                    _logger.LogWarning("Failed to sign up user.");
                    ViewBag.Message = "Failed to sign up user. Please try again.";
                    return View();
                }
            }
            else
            {
                // Model is not valid, return to the signup form with validation errors
                return View(signUpModel);
            }
        }

        public IActionResult Index()
        {
            return View();
        }

        private string GetSha256Hash(string input)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

                // Convert byte array to a string
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
