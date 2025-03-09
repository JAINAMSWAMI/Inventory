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
                    ModelState.AddModelError("ConfirmPassword", "Password and Confirm Password Does not Match");
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

                // Call the InsertSIGNUP method to insert the data into the signup table
                var result = signup.InsertSIGNUP();

                if (result == -1)
                {
                    // User already exists
                    ModelState.AddModelError("Email", "User already registered with this email.");
                    return View(signUpModel);
                }
                else if (result > 0)
                {
                    try
                    {
                        using (SmtpClient smtpClient = new SmtpClient("smtp.gmail.com", 587))
                        {
                            smtpClient.UseDefaultCredentials = false;
                            smtpClient.EnableSsl = true;
                            smtpClient.Credentials = new NetworkCredential("fluxxinventorymanagement@gmail.com", "mply bbmm ochb zwdn");

                            // Create the email message
                            MailMessage mailMessage = new MailMessage
                            {
                                From = new MailAddress("fluxxinventorymanagement@gmail.com"),
                                Subject = "🎉 Welcome to Fluxx Inventory Management! 🎉",
                                IsBodyHtml = true
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
        <h2>Welcome to Fluxx Inventory Management! 🎉</h2>
        <p>Dear <b>{signUpModel.FirstName} {signUpModel.LastName}</b>,</p>
        <p>Your account has been successfully created.</p>
        <a href='https://yourwebsite.com/login' class='button'>Login to Your Account</a>
        <p>Best regards,<br>Fluxx Inventory Management Team</p>
    </div>
</body>
</html>";

                            // Send the email
                            await smtpClient.SendMailAsync(mailMessage);
                        }

                        TempData["SuccessMessage"] = "Registration successful! Please check your email.";
                        return RedirectToAction("Index");
                    }
                    catch (SmtpException smtpEx)
                    {
                        _logger.LogError($"SMTP Error: {smtpEx.StatusCode} - {smtpEx.Message}");
                        TempData["ErrorMessage"] = "Registration successful, but email could not be sent.";
                        return RedirectToAction("Index");
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
