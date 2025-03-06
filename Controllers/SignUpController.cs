using DataLayer;
using Inventory.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Inventory.Controllers
{
    public class SignUpController : Controller
    {
        private readonly ILogger<SignUpController> _logger;

        public ActionResult SignUp()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SignUp(SignUpModel signUpModel)
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
                    try
                    {
                        using (SmtpClient smtpClient = new SmtpClient("smtp.gmail.com", 587))
                        {
                            smtpClient.UseDefaultCredentials = false; // ✅ Prevents conflicts
                            smtpClient.EnableSsl = true;
                            smtpClient.Credentials = new NetworkCredential("fluxxinventorymanagement@gmail.com", "mply bbmm ochb zwdn");

                            // Create the email message
                            MailMessage mailMessage = new MailMessage
                            {
                                From = new MailAddress("fluxxinventorymanagement@gmail.com"),
                                Subject = "🎉 Welcome to Fluxx Inventory Management! 🎉",
                                IsBodyHtml = true // ✅ Enables HTML formatting
                            };

                            // Add recipient
                            mailMessage.To.Add(signUpModel.Email);

                            // Email body with HTML
                            mailMessage.Body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{
            font-family: Arial, sans-serif;
            background-color: #f4f4f4;
            padding: 20px;
            text-align: center;
        }}
        .container {{
            background: white;
            padding: 20px;
            border-radius: 10px;
            max-width: 600px;
            margin: auto;
            box-shadow: 0px 0px 10px #ddd;
        }}
        .logo {{
            width: 150px;
            margin-bottom: 20px;
        }}
        .header {{
            font-size: 24px;
            color: #333;
            font-weight: bold;
        }}
        .content {{
            font-size: 16px;
            color: #555;
            margin-top: 10px;
        }}
        .footer {{
            margin-top: 20px;
            font-size: 14px;
            color: #777;
        }}
        .button {{
            background-color: #28a745;
            color: white;
            padding: 10px 20px;
            text-decoration: none;
            border-radius: 5px;
            display: inline-block;
            margin-top: 15px;
        }}
        .button:hover {{
            background-color: #218838;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <!-- Company Logo -->
        <img src='https://yourwebsite.com/logo.png' alt='Fluxx Inventory' class='logo'>

        <div class='header'>Welcome to Fluxx Inventory Management! 🎉</div>

        <div class='content'>
            <p>Dear <b>{signUpModel.FirstName} {signUpModel.LastName}</b>,</p>
            <p>Thank you for registering with us! We're excited to have you on board.</p>
            <p>Your account has been successfully created.</p>

            <!-- Call-to-action button -->
            <a href='https://yourwebsite.com/login' class='button'>Login to Your Account</a>
        </div>

        <div class='footer'>
            Best regards, <br>
            <b>Fluxx Inventory Management Team</b> <br>
            📧 support@fluxxinventory.com | 📞 +91-9876543210
        </div>
    </div>
</body>
</html>";


                            mailMessage.To.Add(signUpModel.Email);

                            // Send the email
                            await smtpClient.SendMailAsync(mailMessage);
                        }

                        TempData["SuccessMessage"] = "Registration successful! Please check your email.";
                        return RedirectToAction("Index");  // ✅ Redirect after success
                    }
                    catch (SmtpException smtpEx)
                    {
                        _logger.LogError($"SMTP Error: {smtpEx.StatusCode} - {smtpEx.Message}");
                        TempData["ErrorMessage"] = "Registration successful, but email could not be sent.";
                        return RedirectToAction("Index");  // ✅ Still redirect
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Email sending failed: {ex.Message}");
                        ModelState.AddModelError("", "An error occurred while sending the email.");
                        return View(signUpModel);
                    }

                }
                else
                {
                    ModelState.AddModelError("", "Registration failed. Please try again.");
                }
            }

            return View(signUpModel);
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
