using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ChatAISystem.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using ChatAISystem.Helper;
using System.Text.RegularExpressions;
using ChatAISystem.Permissions;

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
        // GET: Character

        public async Task<IActionResult> Index(string searchName, string currentFilter, int? pageNumber)
        {
            // Si se envía un nuevo término de búsqueda, reinicia la paginación
            if (searchName != null)
            {
                pageNumber = 1;
                currentFilter = searchName;
            }
            else
            {
                // No hay nuevo término, usar el filtro actual
                searchName = currentFilter;
            }

            // Guardar el filtro actual para mantenerlo en la vista
            ViewData["CurrentFilter"] = searchName;

            var characterQuery = _context.Characters
                .Include(c => c.CreatedByNavigation)
                .AsQueryable();

            // Aplicar el filtro de búsqueda si existe
            if (!string.IsNullOrEmpty(searchName))
            {
                characterQuery = characterQuery.Where(c => c.Name.Contains(searchName));
            }
            // Definir el tamaño de la página (5 registros por página)
            int pageSize = 4;

            // Utilizar PaginatedList para manejar la paginación
            return View(await Pagination.PaginatedList<Character>.CreateAsync(characterQuery.AsNoTracking(), pageNumber ?? 1, pageSize));
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
                character.Name = Utilities.CleanField(character.Name);
                character.Description = Utilities.CleanField(character.Description);
                character.AvatarUrl = Utilities.CleanField(character.AvatarUrl);
                character.AvatarUrl = Utilities.ValidateLinkImage(character.AvatarUrl) ? character.AvatarUrl : null;
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
            //var character = await _context.Characters.FindAsync(id);

            var character = await _context.Characters
                .Include(c => c.Conversations)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (character == null) {
                return NotFound();
            }
            _context.Conversations.RemoveRange(character.Conversations); // Elimina las conversaciones
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
