using ChatAISystem.Models;
using ChatAISystem.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ChatAISystem.Helper;
using ChatAISystem.Services.Interfaces;

namespace ChatAISystem.Controllers
{
    public class RegisterController : Controller
    {
        private readonly IUserValidationService _userValidationService;
        private readonly ChatAIDBContext _context;
        private readonly IConfiguration _configuration;

        public RegisterController(
            ChatAIDBContext context,
            IConfiguration configuration,
            IUserValidationService userValidationService)
        {
            _context = context;
            _configuration = configuration;
            _userValidationService = userValidationService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(RegisterViewModel model)
        {
            var validationResponse =
                await _userValidationService.ValidateRegistrationAsync(model, Request.Form);

            if (!validationResponse.success)
            {
                return Json(new
                {
                    success = false,
                    message = validationResponse.message
                });
            }

            try
            {
                var user = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    PasswordHash = Utilities.ConverterSha256(model.Password)
                };

                _context.Add(user);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Registration completed successfully.",
                    redirectUrl = Url.Action("Index", "Login")
                });
            }
            catch (DbUpdateException dbEx)
            {
                return Json(new
                {
                    success = false,
                    message = "The email or username is already in use."
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "An unexpected error occurred. Please try again later."
                });
            }
        }
    }
}
