using EducationPlatform.Data;
using EducationPlatform.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace EducationPlatform.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        // Конструктор: тут ми отримуємо доступ до БД
        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Запит до бази: "Дай мені всі курси"
            var courses = await _context.Courses.ToListAsync();
            return View(courses);
        }

        public IActionResult About()
        {
            return View();
        }
        public async Task<IActionResult> Courses()
        {
            // Запит до бази: "Дай мені всі курси"
            var courses = await _context.Courses.ToListAsync();
            return View(courses);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public async Task<IActionResult> CourseDetails(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Lessons) // Важливо! Підвантажуємо уроки цього курсу
                .FirstOrDefaultAsync(m => m.Id == id);

            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        public async Task<IActionResult> Lesson(int id)
        {
            var lesson = await _context.Lessons
                .Include(l => l.Course) // Підтягуємо інфо про курс (щоб знати назву курсу)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lesson == null)
            {
                return NotFound();
            }

            return View(lesson);
        }
    }
}
