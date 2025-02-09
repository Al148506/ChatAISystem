using ChatAISystem.Models;
using ChatAISystem.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using ChatAISystem.Helper;
namespace ChatAISystem.Controllers
{
    public class RegisterController : Controller
    {
        private readonly CharAidbContext _context;
        private readonly IConfiguration _configuration;

        public RegisterController(CharAidbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RegisterAsync(RegisterViewModel model)
        {
            var validationResponse = ValidateRegistration(model);
            if (!validationResponse.success)
            {
                return Json(validationResponse);
            }

            try
            {
                var captchaResponse = Request.Form["g-recaptcha-response"];
                var utilities = new Utilities();
                if (string.IsNullOrEmpty(captchaResponse) || !await utilities.ValidateCaptcha(captchaResponse, _configuration))
                {
                    return Json(new { success = false, message = "Por favor, resuelva el reCAPTCHA para continuar." });
                }
                var user = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    PasswordHash = Utilities.ConverterSha256(model.Password)
                };

                _context.Add(user);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Registro exitoso", redirectUrl = Url.Action("Index","Login") });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error al registrar usuario, intente con otro correo" });
            }
        }

        // ✅ Método de validación reutilizable
        private (bool success, string message) ValidateRegistration(RegisterViewModel model)
        {
       
            if (string.IsNullOrWhiteSpace(model.Email) || !Utilities.IsValidEmail(model.Email))
                return (false, "Por favor, ingrese un correo electrónico válido.");

            if (string.IsNullOrWhiteSpace(model.Username))
                return (false, "Por favor, ingrese un nombre de usuario.");

            if (string.IsNullOrWhiteSpace(model.Password) || model.Password.Length < 6)
                return (false, "La contraseña debe tener al menos 6 caracteres.");

            if (model.Password != model.ConfirmPassword)
                return (false, "Las contraseñas no coinciden.");

            return (true, "Validación exitosa");
        }

    }
}
