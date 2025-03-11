using ChatAISystem.Helper;
using ChatAISystem.Models;
using ChatAISystem.Models.ViewModels;
using ChatAISystem.Permissions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChatAISystem.Controllers
{
    [RoleValidation("Admin")]
    public class UserController : Controller
    {
        private readonly ChatAIDBContext _context;
        public UserController(ChatAIDBContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchName, string currentFilter, int? pageNumber)
        {
            if (searchName != null)
            {
                pageNumber = 1;
                currentFilter = searchName;
            }
            else
            {
                searchName = currentFilter;
            }

            ViewData["CurrentFilter"] = searchName;

            var userQuery = _context.Users
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchName))
            {
                userQuery = userQuery.Where(u => u.Username.Contains(searchName));
            }

            int pageSize = 4;

            return View(await Pagination.PaginatedList<User>.CreateAsync(userQuery.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Username,Email,PasswordHash,Role")] User user)
        {
            if (ModelState.IsValid)
            {
                user.PasswordHash = Utilities.ConverterSha256(user.PasswordHash);
                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            var userViewModel = new AdminRegisterViewModel
            {
                Id = user.Id, // Asegurar que se pasa el Id
                Username = user.Username,
                Email = user.Email,
                Password = user.PasswordHash,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            };
            return View(userViewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Username,Email,PasswordHash,Role")] AdminRegisterViewModel userViewModel)
        {
            if (id != userViewModel.Id)
            {
                return NotFound();
            }
            // 🔍 DEBUG: Ver si hay errores en el ModelState
            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Error: {error.ErrorMessage}");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // 🔹 Obtener el usuario original de la base de datos
                    var existingUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
                    if (existingUser == null)
                    {
                        return NotFound();
                    }

                    // 🔹 Actualizar solo las propiedades permitidas
                    existingUser.Username = userViewModel.Username;
                    existingUser.Email = userViewModel.Email;
                    existingUser.Role = userViewModel.Role;

                    // 🔹 Si la contraseña fue modificada, actualizarla
                    if (!string.IsNullOrWhiteSpace(userViewModel.Password))
                    {
                        existingUser.PasswordHash = Utilities.ConverterSha256(userViewModel.Password);
                    }

                    _context.Update(existingUser);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index)); // Redirige a la lista de usuarios
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: {ex.Message}");
                    throw;
                }
            }

            return View(userViewModel); // Si hay errores de validación, recarga la vista con los datos
        }


        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users
               .Include(c => c.Conversations)
               .FirstOrDefaultAsync(c => c.Id == id);
            if (user == null)
            {
                return NotFound();
            }
            _context.Conversations.RemoveRange(user.Conversations); // Elimina las conversaciones
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
    }
}
