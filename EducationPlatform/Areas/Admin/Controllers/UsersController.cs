using EducationPlatform.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EducationPlatform.Areas.Admin.Controllers // Зверни увагу на namespace
{
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin")] // Доступ ТІЛЬКИ для тебе
    public class UsersController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;

        public UsersController(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        // Сторінка зі списком усіх користувачів
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userRolesList = new List<UserRoleViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRolesList.Add(new UserRoleViewModel
                {
                    UserId = user.Id,
                    Email = user.Email,
                    Roles = roles
                });
            }

            return View(userRolesList);
        }

        // Метод для надання прав Admin (Викладача)
        [HttpPost]
        public async Task<IActionResult> AddAdminRole(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                // Перевіряємо, чи немає в нього вже цієї ролі
                if (!await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    await _userManager.AddToRoleAsync(user, "Admin");
                }
            }
            // Оновлюємо сторінку
            return RedirectToAction(nameof(Index));
        }

        // Метод для зняття прав Admin
        [HttpPost]
        public async Task<IActionResult> RemoveAdminRole(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                // SuperAdmin'а не можна понизити
                if (!await _userManager.IsInRoleAsync(user, "SuperAdmin"))
                {
                    await _userManager.RemoveFromRoleAsync(user, "Admin");
                }
            }
            return RedirectToAction(nameof(Index));
        }
    }
}