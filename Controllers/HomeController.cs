using ChatAISystem.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ChatAISystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
        public IActionResult AccessDenied()
        {
            return View();
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        public IActionResult CloseSession()
        {
            // Limpiar toda la sesión
            HttpContext.Session.Clear();

            // Opcional: Redirigir al usuario a la página de inicio de sesión o de inicio
            return RedirectToAction("Index", "Login");
        }

    }
}
