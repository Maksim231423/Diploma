using EducationPlatform.Data;
using EducationPlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EducationPlatform.Areas.Admin.Controllers
{
    [Area("Admin")] // Кажемо, що це зона адміна
    [Authorize(Roles = "Admin")] // Надає доступ лише адміну
    public class CoursesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CoursesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Показати список курсів
        public async Task<IActionResult> Index()
        {
            return View(await _context.Courses.ToListAsync());
        }

        // 2. Показати сторінку "Створити курс"
        public IActionResult Create()
        {
            return View();
        }

        // 3. Зберегти новий курс (коли натиснеш кнопку Save)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description,Price,ImageUrl")] Course course)
        {
            if (ModelState.IsValid)
            {
                _context.Add(course);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(course);
        }

        // Тут пізніше додамо Edit і Delete, поки вистачить Create
    }
}