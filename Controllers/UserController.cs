using ChatAISystem.Helper;
using ChatAISystem.Models;
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
            return View(user);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Username,Email,PasswordHash,Role")] User user)
        {
            if (id != user.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    user.PasswordHash = Utilities.ConverterSha256(user.PasswordHash);
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.Id))
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
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
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
