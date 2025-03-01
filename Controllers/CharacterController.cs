using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ChatAISystem.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ChatAISystem.Controllers
{
    public class CharacterController : Controller
    {
        private readonly ChatAIDBContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CharacterController(ChatAIDBContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        // GET: Character
        public async Task<IActionResult> Index()
        {
            var chatAIDBContext = _context.Characters.Include(c => c.CreatedByNavigation);
            return View(await chatAIDBContext.ToListAsync());
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Create(Character character)
        {
            ModelState.Remove("CreatedByNavigation");
            if (!ModelState.IsValid)
            {
                foreach (var key in ModelState.Keys)
                {
                    if (ModelState[key]?.Errors != null)
                    {
                        foreach (var error in ModelState[key].Errors)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error en {key}: {error.ErrorMessage}");
                        }
                    }
                }
            }

            if (ModelState.IsValid)
            {
                var session = _httpContextAccessor.HttpContext?.Session;
                int? userId = session?.GetInt32("IdUser");
                if (userId == null)
                {
                    return Unauthorized(); // Devuelve error si no hay usuario autenticado
                }

                character.CreatedAt = DateTime.UtcNow;
                character.CreatedBy = userId.Value;
                _context.Add(character);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index", "Character");
            }

            // Si llegamos aquí, ModelState no es válido. Se devuelve la vista para mostrar los errores.
            return View(character);
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var character = await _context.Characters.FindAsync(id);
            if (character == null)
            {
                return NotFound();
            }
            ViewData["CreatedByEmail"] = _context.Users
            .Where(u => u.Id == character.CreatedBy)
            .Select(u => u.Email)
            .FirstOrDefault();
            return View(character);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(int id, Character character)
        {
            if (id != character.Id)
            {
                return NotFound();
            }

            ModelState.Remove("CreatedByNavigation");
            if (!ModelState.IsValid)
            {
                foreach (var key in ModelState.Keys)
                {
                    if (ModelState[key]?.Errors != null)
                    {
                        foreach (var error in ModelState[key].Errors)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error en {key}: {error.ErrorMessage}");
                        }
                    }
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(character);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CharacterExists(character.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CreatedByEmail"] = _context.Users
            .Where(u => u.Id == character.CreatedBy)
            .Select(u => u.Email)
            .FirstOrDefault();
            return View(character);
        }
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var character = await _context.Characters
                .Include(c => c.CreatedByNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (character == null)
            {
                return NotFound();
            }

            return View(character);
        }
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var character = await _context.Characters.FindAsync(id);
            _context.Characters.Remove(character);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }



        private bool CharacterExists(int id)
        {
            return _context.Characters.Any(e => e.Id == id);
        }
    }
}
