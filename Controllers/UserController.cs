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

        public async Task<IActionResult> Index(string searchName, string currentFilter, int? numpag)
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

            var userQuery = _context.Users
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchName))
            {
                userQuery = userQuery.Where(u => u.Username.Contains(searchName));
            }

            int regQuantity = 4;

            return View(await Pagination<User>.CreatePagination(userQuery.AsNoTracking(), numpag ?? 1, regQuantity));
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Username,Email,Password,Role")] AdminRegisterViewModel model)
        {
            if (_context.Users.Any(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "This email has already been registered.");
            }
            if (!ModelState.IsValid)
            {
                // Muestra los errores en la consola de depuración
                foreach (var error in ModelState)
                {
                    foreach (var subError in error.Value.Errors)
                    {
                        Console.WriteLine($"Error en {error.Key}: {subError.ErrorMessage}");
                    }
                }

                // Devuelve la vista con los datos para que el usuario corrija los errores
                return View(model);
            }

            var user = new User
            {
                Username = model.Username,
                Email = model.Email,
                PasswordHash = Utilities.ConverterSha256(model.Password),
                Role = model.Role
            };

            _context.Add(user);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            var userViewModel = new AdminEditViewModel
            {
                Id = user.Id, // Asegurar que se pasa el Id
                Username = user.Username,
                Email = user.Email,
                Password = user.PasswordHash,
                Role = user.Role,

            };
            return View(userViewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Username,Email,PasswordHash,Role")] AdminEditViewModel model)
        {
            if (_context.Users.Any(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "This email has already been registered.");
            }
            if (!ModelState.IsValid)
            {
                // Muestra los errores en la consola de depuración
                foreach (var error in ModelState)
                {
                    foreach (var subError in error.Value.Errors)
                    {
                        Console.WriteLine($"Error en {error.Key}: {subError.ErrorMessage}");
                    }
                }

                // Devuelve la vista con los datos para que el usuario corrija los errores
                return View(model);
            }

             
            // 🔹 Obtener el usuario original de la base de datos
                    var existingUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
                    if (existingUser == null)
                    {
                        return NotFound();
                    }

                    // 🔹 Actualizar solo las propiedades permitidas
                    existingUser.Username = model.Username;
                    existingUser.Email = model.Email;
                    existingUser.Role = model.Role;

                    // 🔹 Si la contraseña fue modificada, actualizarla
                    if (!string.IsNullOrWhiteSpace(model.Password))
                    {
                        existingUser.PasswordHash = Utilities.ConverterSha256(model.Password);
                    }

                    _context.Update(existingUser);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index)); // Redirige a la lista de usuario
        
            
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

        [HttpGet]
        public JsonResult IsEmailAvailable(string email)
        {
            bool exists = _context.Users.Any(u => u.Email == email);
            return Json(!exists); // Devuelve true si el email NO existe, false si ya está registrado
        }
    }
}
