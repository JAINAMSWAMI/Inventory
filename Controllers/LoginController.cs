using DataLayer;
using Inventory.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Inventory.Controllers
{
    public class LoginController : Controller
    {
        private readonly ILogger<LoginController> _logger;
        private readonly IConfiguration _configuration;

        public LoginController(ILogger<LoginController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
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
                _logger.LogInformation("Login attempt started for user: " + model.Email);

                // Create a new Login object and set its properties
                Login login = new Login
                {
                    Email = model.Email,
                    Password = EncodePassword(model.Password)
                };

                _logger.LogInformation("Password encoded for user: " + model.Email);

                // Check if the user exists and log them in
                if (login.LoginUser())
                {
                    _logger.LogInformation("User found and logged in successfully.");

                    // Generate JWT token
                    string token = GenerateJwtToken(login.Email);
                    _logger.LogInformation("JWT token generated for user: " + model.Email);

                    // Create session using a GUID and set it in the cookie
                    string sessionID = Guid.NewGuid().ToString();
                    HttpContext.Response.Cookies.Append("SessionId", sessionID, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        Expires = DateTime.UtcNow.AddMinutes(30)
                    });

                    _logger.LogInformation("Session created with ID: " + sessionID);

                    // Redirect to Index page after successful login
                    return RedirectToAction("Index", "SignUp");
                }
                else
                {
                    _logger.LogWarning("Login failed for user: " + model.Email);
                    ViewBag.Message = "INVALID EMAIL OR PASSWORD.";
                    return View(); // Return the login view with error message
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while trying to log in. Exception details: {Message}", ex.Message);
                return StatusCode(500, new { Message = "An error occurred. Please try again later." });
            }
        }


        // Method to generate a JWT token
        private string GenerateJwtToken(string email)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]);
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];
            var expiration = int.Parse(jwtSettings["ExpirationInMinutes"]);

            var claims = new[]
            {
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, "User") // You can customize the role or add more claims
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(expiration),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(secretKey), SecurityAlgorithms.HmacSha256)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
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
