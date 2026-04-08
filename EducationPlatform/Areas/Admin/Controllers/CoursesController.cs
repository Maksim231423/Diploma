using EducationPlatform.Data;
using EducationPlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EducationPlatform.Models.ViewModels;

namespace EducationPlatform.Areas.Admin.Controllers
{
    [Area("Admin")] // Кажемо, що це зона адміна
    [Authorize(Roles = "SuperAdmin, Admin")] // Надає доступ лише адміну
    public class CoursesController : Controller
    {

        private readonly ApplicationDbContext _context;

        public CoursesController(ApplicationDbContext context)
        {
            _context = context;
        }

        //Головна сторінка, дашборд
        public async Task<IActionResult> Index()
        {
            // 1. Рахуємо студентів (всі зареєстровані користувачі)
            int studentsCount = await _context.Users.CountAsync();

            // 2. Рахуємо активні курси
            int coursesCount = await _context.Courses.CountAsync();

            // 3. Рахуємо неперевірені домашки (де IsChecked == false)
            int newHomeworksCount = await _context.HomeworkSubmissions.CountAsync(h => !h.IsChecked);

            // 4. Рахуємо дохід за поточний місяць
            // Отримуємо перше число поточного місяця (наприклад, 1 квітня 2026)
            var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

            // Шукаємо всі покупки за цей місяць і сумуємо ціни курсів
            decimal income = await _context.Purchases
                .Include(p => p.Course) // Підтягуємо курс, щоб дізнатися його ціну
                .Where(p => p.PurchaseDate >= startOfMonth)
                .SumAsync(p => p.Course.Price);

            // 5. Пакуємо все у ViewModel
            var model = new AdminDashboardViewModel
            {
                TotalStudents = studentsCount,
                ActiveCourses = coursesCount,
                NewHomeworks = newHomeworksCount,
                MonthlyIncome = income
            };

            return View(model);
        }

        // ГОЛОВНА СТОРІНКА УПРАВЛІННЯ КУРСАМИ
        [HttpGet]
        public async Task<IActionResult> Courses() // Назва має збігатися з asp-action="Courses"
        {
            // 1. Дістаємо курси з бази разом з уроками та тегами
            var courses = await _context.Courses
                .Include(c => c.Lessons)
                .Include(c => c.Tags)
                .ToListAsync();

            // 2. Передаємо ці курси у View
            return View(courses);
        }

        // 3. Сторінка "Домашні завдання"
        public IActionResult Homeworks()
        {
            return View();
        }

        // 4. Сторінка "Email розсилка"
        public IActionResult Newsletter()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            // Знаходимо курс у базі
            var course = await _context.Courses.FindAsync(id);

            if (course != null)
            {
                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();

                // Записуємо повідомлення про успішне видалення
                TempData["SuccessMessage"] = $"Курс '{course.Title}' було успішно видалено.";
            }

            // Повертаємось на сторінку таблиці
            return RedirectToAction("Courses");
        }

        [HttpGet]
        public async Task<IActionResult> EditCourse(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Tags)
                .Include(c => c.Lessons) // <--- ДОДАНО: Підтягуємо уроки
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null) return NotFound();

            var model = new CourseEditViewModel
            {
                Id = course.Id,
                Title = course.Title,
                Description = course.Description,
                Price = course.Price,
                ImageUrl = course.ImageUrl,
                SelectedTags = course.Tags.Select(t => t.Id).ToList(),
                // ДОДАНО: Сортуємо уроки за номером і передаємо у модель
                Lessons = course.Lessons.OrderBy(l => l.OrderNumber).Select(l => new LessonViewModel
                {
                    Id = l.Id,
                    Title = l.Title,
                    OrderNumber = l.OrderNumber,
                    VideoUrl = l.VideoUrl,
                    HomeworkDescription = l.HomeworkDescription
                }).ToList()
            };

            ViewBag.Tags = new MultiSelectList(await _context.Tags.ToListAsync(), "Id", "Name");
            return View(model);
        }

        // ==========================================
        // ЛОГІКА КЕРУВАННЯ УРОКАМИ
        // ==========================================

        [HttpPost]
        public async Task<IActionResult> AddLesson(int courseId, string title, int orderNumber, string videoUrl, string homeworkDescription)
        {
            // Створюємо новий урок
            var lesson = new Lesson
            {
                CourseId = courseId,
                Title = title,
                OrderNumber = orderNumber,
                VideoUrl = videoUrl,
                HomeworkDescription = homeworkDescription
            };

            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Урок успішно додано!";

            // Повертаємось на цю ж сторінку редагування курсу
            return RedirectToAction("EditCourse", new { id = courseId });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteLesson(int lessonId, int courseId)
        {
            var lesson = await _context.Lessons.FindAsync(lessonId);
            if (lesson != null)
            {
                _context.Lessons.Remove(lesson);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Урок успішно видалено!";
            }

            return RedirectToAction("EditCourse", new { id = courseId });
        }

        [HttpGet]
        public async Task<IActionResult> CreateCourse()
        {
            // Створюємо порожню модель для нової форми
            var model = new CourseEditViewModel
            {
                Id = 0,
                Title = "",
                Description = "",
                Price = 0,
                ImageUrl = ""
            };

            // Передаємо список всіх тегів для вибору
            ViewBag.Tags = new MultiSelectList(await _context.Tags.ToListAsync(), "Id", "Name");

            // Кажемо контролеру повернути файл "EditCourse.cshtml"
            return View("EditCourse", model);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCourse(CourseEditViewModel input)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Tags = new MultiSelectList(await _context.Tags.ToListAsync(), "Id", "Name");
                return View("EditCourse", input);
            }

            var newCourse = new Course
            {
                Title = input.Title,
                Description = input.Description,
                Price = input.Price,
                ImageUrl = input.ImageUrl
            };

            if (input.SelectedTags != null && input.SelectedTags.Any())
            {
                newCourse.Tags = await _context.Tags.Where(t => input.SelectedTags.Contains(t.Id)).ToListAsync();
            }

            // ДОДАНО: Відразу зберігаємо всі створені в таблиці уроки
            if (input.Lessons != null && input.Lessons.Any())
            {
                newCourse.Lessons = input.Lessons.Select(l => new Lesson
                {
                    Title = l.Title,
                    OrderNumber = l.OrderNumber,
                    VideoUrl = l.VideoUrl,
                    HomeworkDescription = l.HomeworkDescription
                }).ToList();
            }

            _context.Courses.Add(newCourse);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Курс '{newCourse.Title}' успішно створено!";
            return RedirectToAction("Courses");
        }

        [HttpPost]
        public async Task<IActionResult> EditCourse(CourseEditViewModel input)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Tags = new MultiSelectList(await _context.Tags.ToListAsync(), "Id", "Name");
                return View("EditCourse", input);
            }

            var course = await _context.Courses.Include(c => c.Tags).Include(c => c.Lessons).FirstOrDefaultAsync(c => c.Id == input.Id);
            if (course == null) return NotFound();

            course.Title = input.Title;
            course.Description = input.Description;
            course.Price = input.Price;
            course.ImageUrl = input.ImageUrl;

            course.Tags = await _context.Tags.Where(t => input.SelectedTags.Contains(t.Id)).ToListAsync();

            // ДОДАНО: Синхронізація уроків (Smart Update)
            var incomingIds = input.Lessons?.Select(l => l.Id).ToList() ?? new List<int>();

            // 1. Видаляємо з бази ті уроки, які адмін видалив з таблиці на фронтенді
            var lessonsToRemove = course.Lessons.Where(l => !incomingIds.Contains(l.Id)).ToList();
            _context.Lessons.RemoveRange(lessonsToRemove);

            // 2. Додаємо нові або оновлюємо існуючі
            if (input.Lessons != null)
            {
                foreach (var item in input.Lessons)
                {
                    if (item.Id == 0) // Це новий урок, доданий щойно
                    {
                        course.Lessons.Add(new Lesson
                        {
                            Title = item.Title,
                            OrderNumber = item.OrderNumber,
                            VideoUrl = item.VideoUrl,
                            HomeworkDescription = item.HomeworkDescription
                        });
                    }
                    else // Це старий урок, оновлюємо дані
                    {
                        var existing = course.Lessons.FirstOrDefault(l => l.Id == item.Id);
                        if (existing != null)
                        {
                            existing.Title = item.Title;
                            existing.OrderNumber = item.OrderNumber;
                            existing.VideoUrl = item.VideoUrl;
                            existing.HomeworkDescription = item.HomeworkDescription;
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Курс '{course.Title}' успішно оновлено!";
            return RedirectToAction("Courses");
        }

    }
}