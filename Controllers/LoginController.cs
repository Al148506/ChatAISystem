using ChatAISystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ChatAISystem.Helper;

namespace ChatAISystem.Controllers
{
    public class LoginController : Controller
    {
        private readonly ChatAIDBContext _context;
        private readonly IConfiguration _configuration;

        public LoginController(ChatAIDBContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ValidateLogin(User model)
        {
            try
            {
                // Check if reCAPTCHA was completed
                var captchaResponse = Request.Form["g-recaptcha-response"];
                var utilities = new Utilities();

                if (string.IsNullOrEmpty(captchaResponse) ||
                    !await utilities.ValidateCaptcha(captchaResponse, _configuration))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Please complete the reCAPTCHA to continue."
                    });
                }

                // Validate credentials
                if (string.IsNullOrWhiteSpace(model.Email) ||
                    string.IsNullOrWhiteSpace(model.PasswordHash))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Please enter both email and password."
                    });
                }

                model.Email = model.Email.Trim();
                model.PasswordHash = Utilities.ConverterSha256(model.PasswordHash);

                var result = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r =>
                        r.Email == model.Email &&
                        r.PasswordHash == model.PasswordHash);

                if (result == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Invalid email or password."
                    });
                }

                // Store session and redirect
                HttpContext.Session.Clear();
                HttpContext.Session.SetInt32("IdUser", result.Id);
                HttpContext.Session.SetString("UserMail", result.Email);
                HttpContext.Session.SetString("UserName", result.Username);
                HttpContext.Session.SetString("Role", result.Role);

                HttpContext.Response.Cookies.Append(
                    "SessionSecurity",
                    Guid.NewGuid().ToString(),
                    new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true
                    });

                return Json(new
                {
                    success = true,
                    redirectUrl = Url.Action("Index", "Home")
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return Json(new
                {
                    success = false,
                    message = "An unexpected error occurred. Please try again."
                });
            }
        }
    }
}
