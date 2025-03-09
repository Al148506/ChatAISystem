using ChatAISystem.Models;
using ChatAISystem.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using ChatAISystem.Helper;
using Microsoft.Extensions.Configuration;
namespace ChatAISystem.Controllers
{
    public class RegisterController : Controller
    {
        private readonly ChatAIDBContext _context;
        private readonly IConfiguration _configuration;

        public RegisterController(ChatAIDBContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(RegisterViewModel model)
        {
            var validationResponse = await ValidateRegistration(model);
            if (!validationResponse.success)
            {
                return Json(new { success = false, message = validationResponse.message });
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

                return Json(new { success = true, message = "Registro exitoso", redirectUrl = Url.Action("Index", "Login") });
            }
            catch (DbUpdateException dbEx) // 🔥 Capturar errores de base de datos
            {
                Console.WriteLine($"Error en la base de datos: {dbEx.Message}");
                return Json(new { success = false, message = "El correo o usuario ya están en uso." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inesperado: {ex.Message}");
                return Json(new { success = false, message = "Ocurrió un error inesperado. Inténtelo más tarde." });
            }
        }


        private async Task<(bool success, string message)> ValidateRegistration(RegisterViewModel model)
        {
            if (model == null)
            {
                return (false, "Datos inválidos.");
            }

            var captchaResponse = Request.Form["g-recaptcha-response"];

            if (string.IsNullOrEmpty(captchaResponse))
            {
                return (false, "Captcha no encontrado en el formulario.");
            }

            var utilities = new Utilities();
            var isCaptchaValid = await utilities.ValidateCaptcha(captchaResponse, _configuration);
            if (!isCaptchaValid)
            {
                return (false, "Por favor, resuelva el reCAPTCHA para continuar.");
            }

            if (string.IsNullOrWhiteSpace(model.Username) || string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
            {
                return (false, "Por favor, ingrese todos los datos.");
            }

            if (!Utilities.IsValidEmail(model.Email))
            {
                return (false, "Ingrese un correo electrónico válido.");
            }

            if (model.Password.Length < 6)
            {
                return (false, "La contraseña debe tener al menos 6 caracteres.");
            }

            if (model.Password != model.ConfirmPassword)
            {
                return (false, "Las contraseñas no coinciden.");
            }

            return (true, "Validación exitosa");
        }
    }
}


