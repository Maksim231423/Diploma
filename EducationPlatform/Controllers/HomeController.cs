using EducationPlatform.Data;
using EducationPlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity; // Для роботи з користувачами
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace EducationPlatform.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager; // Додаємо менеджер користувачів

        // Оновлюємо конструктор, щоб отримати UserManager
        public HomeController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // Додаємо .Include(c => c.Tags), щоб база даних одразу віддала нам і теги також
            var courses = await _context.Courses
                .Include(c => c.Tags)
                .AsNoTracking()
                .ToListAsync();

            return View(courses);
        }

        public async Task<IActionResult> Courses(string searchQuery, string selectedTags)
        {
            var query = _context.Courses.Include(c => c.Tags).AsNoTracking().AsQueryable();

            // Перетворюємо рядок "C#,SQL" назад у список ["C#", "SQL"]
            var tagsList = new List<string>();
            if (!string.IsNullOrEmpty(selectedTags))
            {
                tagsList = selectedTags.Split(',').ToList();
            }

            if (!string.IsNullOrEmpty(searchQuery))
            {
                query = query.Where(c => c.Title.Contains(searchQuery) || c.Description.Contains(searchQuery));
                ViewBag.SearchQuery = searchQuery;
            }

            // Фільтруємо по всім обраним тегам
            if (tagsList.Any())
            {
                foreach (var tag in tagsList)
                {
                    query = query.Where(c => c.Tags.Any(t => t.Name == tag));
                }
            }

            ViewBag.SelectedTags = tagsList; // Зберігаємо список для відображення
            ViewBag.AllTags = await _context.Tags.Select(t => t.Name).Distinct().ToListAsync();

            var courses = await query.ToListAsync();
            return View(courses);
        }

        public IActionResult About()
        {
            return View();
        }

        // ОНОВЛЕНИЙ МЕТОД ДЕТАЛЕЙ КУРСУ
        public async Task<IActionResult> CourseDetails(int id)
        {

            var course = await _context.Courses
                .Include(c => c.Lessons)
                .Include(c => c.Comments)
                .ThenInclude(coment => coment.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (course == null)
            {
                return NotFound();
            }

            // ПЕРЕВІРКА ОПЛАТИ
            bool isPurchased = false;
            var user = await _userManager.GetUserAsync(User); // Отримуємо поточного юзера

            if (user != null)
            {
                // Перевіряємо, чи є запис у таблиці Purchase для цього юзера і курсу
                isPurchased = await _context.Purchases
                    .AnyAsync(p => p.CourseId == id && p.UserId == user.Id);
            }

            // Передаємо статус покупки через ViewBag, щоб використати його у View
            ViewBag.IsPurchased = isPurchased;

            return View(course);
        }

        [HttpPost]
        [Authorize] // Тільки авторизовані користувачі
        public async Task<IActionResult> AddComment(int courseId, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return RedirectToAction("CourseDetails", new { id = courseId });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Перевіряємо, чи купив цей користувач цей курс
            bool isPurchased = await _context.Purchases
                .AnyAsync(p => p.CourseId == courseId && p.UserId == user.Id);

            if (!isPurchased)
            {
                // Якщо хтось спробує надіслати запит напряму в обхід браузера
                TempData["AccessDenied"] = "Тільки студенти, які придбали курс, можуть залишати відгуки 🔒";
                return RedirectToAction("CourseDetails", new { id = courseId });
            }

            var comment = new Comment
            {
                CourseId = courseId,
                UserId = user.Id,
                Text = text,
                CreatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return RedirectToAction("CourseDetails", new { id = courseId });
        }

        public async Task<IActionResult> Lesson(int id)
        {
            var lesson = await _context.Lessons.FirstOrDefaultAsync(l => l.Id == id);
            if (lesson == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            bool isPurchased = false;

            // ДОДАЄМО ЗМІННУ ДЛЯ ПЕРЕВІРКИ ДОМАШКИ
            bool hasSubmitted = false;

            if (user != null)
            {
                // Перевіряємо покупку
                isPurchased = await _context.Purchases
                    .AnyAsync(p => p.CourseId == lesson.CourseId && p.UserId == user.Id);

                // ПЕРЕВІРЯЄМО, ЧИ ВЖЕ ВІДПРАВИВ ДОМАШКУ
                hasSubmitted = await _context.HomeworkSubmissions
                    .AnyAsync(h => h.LessonId == id && h.UserId == user.Id);
            }

            if (!isPurchased)
            {
                TempData["AccessDenied"] = "Щоб переглядати матеріали уроків, необхідно спочатку придбати цей курс 🔒";
                return RedirectToAction("CourseDetails", new { id = lesson.CourseId });
            }

            // Передаємо результат на сторінку
            ViewBag.HasSubmitted = hasSubmitted;

            // ШУКАЄМО НАСТУПНИЙ УРОК
            var nextLesson = await _context.Lessons
                .Where(l => l.CourseId == lesson.CourseId && l.OrderNumber > lesson.OrderNumber)
                .OrderBy(l => l.OrderNumber)
                .FirstOrDefaultAsync();

            // Передаємо Id наступного уроку (якщо він є) у ViewBag
            ViewBag.NextLessonId = nextLesson?.Id;


            // Отримуємо ID поточного користувача
            var userId = _userManager.GetUserId(User);
            bool allHomeworksCompleted = true; // За замовчуванням true (на випадок, якщо домашок взагалі немає)

            if (userId != null)
            {
                // 1. Шукаємо всі уроки в ЦЬОМУ курсі, де Є домашнє завдання
                var lessonsWithHomework = await _context.Lessons
                    .Where(l => l.CourseId == lesson.CourseId && !string.IsNullOrEmpty(l.HomeworkDescription))
                    .Select(l => l.Id)
                    .ToListAsync();

                if (lessonsWithHomework.Any())
                {
                    // 2. Шукаємо всі здані домашки ЦЬОГО користувача для ЦИХ уроків
                    var userSubmissions = await _context.HomeworkSubmissions
                        .Where(h => h.UserId == userId && lessonsWithHomework.Contains(h.LessonId))
                        .Select(h => h.LessonId)
                        .Distinct()
                        .ToListAsync();

                    // 3. Якщо кількість уроків з домашками співпадає з кількістю зданих робіт — все ок!
                    allHomeworksCompleted = lessonsWithHomework.Count == userSubmissions.Count;
                }

                var existingCertificate = await _context.Certificates
                .FirstOrDefaultAsync(c => c.UserId == userId && c.CourseId == lesson.CourseId);

                if (existingCertificate != null)
                {
                    // Якщо сертифікат є, передаємо його унікальний токен на сторінку
                    ViewBag.CertificateToken = existingCertificate.UniqueToken;
                }
            }

            // Передаємо результат у View
            ViewBag.AllHomeworksCompleted = allHomeworksCompleted;

            return View(lesson);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        [Authorize] // Тільки авторизовані користувачі можуть здавати домашку
        public async Task<IActionResult> SubmitHomework(int lessonId, string solutionLink)
        {
            // 1. Отримуємо поточного студента
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge(); // Якщо раптом не авторизований - на сторінку логіну
            }

            // 2. Додаткова перевірка, чи посилання не пусте (хоча HTML 'required' теж працює)
            if (string.IsNullOrWhiteSpace(solutionLink))
            {
                // Якщо пусте - повертаємо назад
                return RedirectToAction("Lesson", new { id = lessonId });
            }

            // 3. Створюємо новий запис для бази даних
            var submission = new HomeworkSubmission
            {
                LessonId = lessonId,
                UserId = user.Id,
                SolutionLink = solutionLink,
                SubmissionDate = DateTime.UtcNow,
                IsChecked = false // За замовчуванням ще не перевірено адміністратором
            };

            // 4. Зберігаємо в базу
            _context.HomeworkSubmissions.Add(submission);
            await _context.SaveChangesAsync();

            // 5. Записуємо повідомлення про успіх і повертаємо студента на сторінку уроку
            TempData["HomeworkSuccess"] = "Ваше рішення успішно відправлено на перевірку!";

            return RedirectToAction("Lesson", new { id = lessonId });
        }

        [HttpPost]
        [Authorize] // Тільки для авторизованих
        public async Task<IActionResult> MarkNotificationsAsRead([FromServices] ApplicationDbContext context, [FromServices] Microsoft.AspNetCore.Identity.UserManager<Microsoft.AspNetCore.Identity.IdentityUser> userManager)
        {
            var userId = userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            // Знаходимо всі непрочитані сповіщення користувача
            var unreadNotifs = await context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            // Робимо їх прочитаними
            foreach (var notif in unreadNotifs)
            {
                notif.IsRead = true;
            }

            await context.SaveChangesAsync();
            return Ok();
        }

    }
}