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
            var captchaResponse = Request.Form["g-recaptcha-response"];
            var utilities = new Utilities();
            if (string.IsNullOrEmpty(captchaResponse) || !await utilities.ValidateCaptcha(captchaResponse, _configuration))
            {
                return Json(new { success = false, message = "Por favor, resuelva el reCAPTCHA para continuar." });
            }

            // Validar si el correo está vacío o tiene formato incorrecto
            if (string.IsNullOrWhiteSpace(model.Email) || !Utilities.IsValidEmail(model.Email))
            {
                return Json(new { success = false, message = "Por favor, ingrese un correo electrónico válido." });
            }

            // Validar si la contraseña está vacía o no coincide
            if (string.IsNullOrWhiteSpace(model.Password) || model.Password != model.ConfirmPassword)
            {
                return Json(new { success = false, message = "Por favor, ingrese una contraseña válida y asegúrese de que coincida con la confirmación." });
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var user = new User()
                    {
                        Username = model.Username,
                        Email = model.Email,
                        PasswordHash = Utilities.ConverterSha256(model.Password)
                    };
                    _context.Add(user);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    // Captura de errores y registro del mensaje
                    ModelState.AddModelError(string.Empty, $"Error al guardar el producto: {ex.Message}");
                    return View(model);
                }
            }
            else
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Error: {error.ErrorMessage}");
                }
                return View(model);
            }
        }
    }
}
