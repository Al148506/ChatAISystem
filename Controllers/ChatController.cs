using ChatAISystem.Models;
using ChatAISystem.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Http;

namespace ChatAISystem.Controllers
{
    public class ChatController : Controller
    {
        private readonly ChatAIDBContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatController(ChatAIDBContext context, IHttpContextAccessor httpContextAccessor, IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _hubContext = hubContext;
        }

        public async Task<IActionResult> Index()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                return Unauthorized();
            }

            int? userId = httpContext.Session.GetInt32("IdUser");
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            User? user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
            {
                return Unauthorized();
            }

            ChatViewModel chatViewModel = new ChatViewModel
            {
                UserId = user.Id,
                Characters = await _context.Characters
                    //.Where(c => c.CreatedBy == user.Id)
                    .ToListAsync()
            };

            return View(chatViewModel);
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage(int userId, int characterId, string message)
        {
            var session = _httpContextAccessor.HttpContext.Session;

            var sessionUserId = session.GetInt32("UserId"); // Obtén el ID de la sesión

            if (sessionUserId == null || sessionUserId != userId)
            {
                throw new InvalidOperationException("Usuario no autenticado.");
            }
            // Enviar mensaje al cliente de SignalR
            await _hubContext.Clients.All.SendAsync("ReceiveMessage", "user", message);
            return Ok();
        }

        // Endpoint para negociar la conexión con Azure SignalR
        [Route("api/negotiate")]
        public IActionResult Negotiate()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var  userId = httpContext?.Session.GetString("Username") ?? "anonymous";
            var url = $"{Request.Scheme}://{Request.Host}/chatHub";

            return Ok(new
            {
                url,
                accessToken = userId
            });
        }
    }
}
