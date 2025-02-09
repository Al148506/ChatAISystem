using Microsoft.AspNetCore.Mvc;

namespace ChatAISystem.Controllers
{
    public class ChatController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
