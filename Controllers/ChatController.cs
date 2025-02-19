using ChatAISystem.Models;
using ChatAISystem.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChatAISystem.Controllers
{
    public class ChatController : Controller
    {
        private readonly ChatAIDBContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ChatController(ChatAIDBContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }
        public async Task<IActionResult> Index()
        {
            int? userId = _httpContextAccessor.HttpContext.Session.GetInt32("IdUser");
            if (!userId.HasValue)
            {
                return Unauthorized();
            }
            User user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
            {
                return Unauthorized();
            }

            ChatViewModel chatViewModel = new ChatViewModel
            {
                UserId = user.Id,
                Characters = await _context.Characters
                    .Where(c => c.CreatedBy == user.Id)
                    .ToListAsync()
            };

            return View(chatViewModel);
        }

    }
}
