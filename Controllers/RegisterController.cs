using ChatAISystem.Models;
using ChatAISystem.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using ChatAISystem.Helper;
using Microsoft.Extensions.Configuration;
using ChatAISystem.Services.Interfaces;
namespace ChatAISystem.Controllers
{
    public class RegisterController : Controller
    {
        private readonly IUserValidationService _userValidationService;
        private readonly ChatAIDBContext _context;
        private readonly IConfiguration _configuration;

        public RegisterController(ChatAIDBContext context, IConfiguration configuration, IUserValidationService userValidationService)
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
            var validationResponse = _userValidationService.ValidateRegistrationAsync(model, Request.Form);
            if (!validationResponse.Result.success)
            {
                return Json(new { success = false, message = validationResponse.Result.message});
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

    }
}


