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
        [HttpGet]
        public async Task<IActionResult> Index(string searchName, string sortOrder, string alpOrder, string currentFilter, int? numpag)
        {
            if (searchName != null)
            {
                numpag = 1;
                currentFilter = searchName;
            }
            else
            {
                searchName = currentFilter;
            }

            ViewData["CurrentFilter"] = searchName;
            ViewData["SortOrder"] = sortOrder;
            ViewData["AlpOrder"] = alpOrder;

            var characterQuery = _context.Characters
                .Include(c => c.CreatedByNavigation)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchName))
            {
                characterQuery = characterQuery.Where(c => c.Name.Contains(searchName));
            }

            characterQuery = sortOrder switch
            {
                "asc" => characterQuery.OrderBy(c => c.CreatedAt),
                "desc" => characterQuery.OrderByDescending(c => c.CreatedAt),
                _ => characterQuery.OrderBy(c => c.CreatedAt),
            };

            characterQuery = alpOrder switch
            {
                "asc" => characterQuery.OrderBy(c => c.Name),
                "desc" => characterQuery.OrderByDescending(c => c.Name),
                _ => characterQuery.OrderBy(c => c.Name),
            };

            int regQuantity = 4;

            return View(await Pagination<Character>.CreatePagination(characterQuery.AsNoTracking(), numpag ?? 1, regQuantity));
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
                return View(character);
            }

            var session = _httpContextAccessor.HttpContext?.Session;
            int? userId = session?.GetInt32("IdUser");
            if (userId == null)
            {
                return Unauthorized();
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

            ViewData["CreatedByEmail"] = await _context.Users
                .Where(u => u.Id == character.CreatedBy)
                .Select(u => u.Email)
                .FirstOrDefaultAsync();

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
                return View(character);
            }

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
            var character = await _context.Characters
                .Include(c => c.Conversations)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (character == null)
            {
                return NotFound();
            }

            _context.Conversations.RemoveRange(character.Conversations);
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
