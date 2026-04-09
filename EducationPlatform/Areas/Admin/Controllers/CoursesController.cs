using EducationPlatform.Data;
using EducationPlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EducationPlatform.Models.ViewModels;
using Microsoft.AspNetCore.Identity.UI.Services;
using EducationPlatform.Services;

namespace EducationPlatform.Areas.Admin.Controllers
{
    [Area("Admin")] // Кажемо, що це зона адміна
    [Authorize(Roles = "SuperAdmin, Admin")] // Надає доступ лише адміну
    public class CoursesController : Controller
    {
        private readonly IEmailSender _emailSender;
        private readonly ApplicationDbContext _context;

        public CoursesController(ApplicationDbContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
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

            // ДОДАНО: Передаємо список курсів для випадаючого списку розсилки
            ViewBag.Courses = await _context.Courses.ToListAsync();
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
        [HttpGet]
        public async Task<IActionResult> Homeworks()
        {
            // Дістаємо всі неперевірені домашки
            // Підтягуємо користувача (щоб знати ім'я та email) та урок з курсом
            var submissions = await _context.HomeworkSubmissions
                .Include(h => h.User)
                .Include(h => h.Lesson)
                    .ThenInclude(l => l.Course)
                .Where(h => !h.IsChecked)
                .OrderBy(h => h.SubmissionDate) // Найстаріші зверху
                .ToListAsync();

            return View(submissions);
        }

        [HttpPost]
        public async Task<IActionResult> SendFeedback(int submissionId, string feedbackMessage)
        {
            // Знаходимо домашку (підтягуємо також Урок і Користувача)
            var submission = await _context.HomeworkSubmissions
                .Include(h => h.User)
                .Include(h => h.Lesson)
                .FirstOrDefaultAsync(h => h.Id == submissionId);

            if (submission != null)
            {
                // 1. Позначаємо завдання як перевірене
                submission.IsChecked = true;

                // =======================================================
                // 2. ДОДАЄМО СПОВІЩЕННЯ НА САЙТІ (ДЛЯ ДЗВІНОЧКА)
                // =======================================================
                if (submission.User != null)
                {
                    var notification = new Notification
                    {
                        UserId = submission.User.Id, // ID студента
                        Message = $"Ваше домашнє завдання до уроку '{submission.Lesson?.Title}' було перевірено ментором!",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Notifications.Add(notification);
                }

                // 3. Зберігаємо всі зміни в базу даних (і статус домашки, і нове сповіщення)
                await _context.SaveChangesAsync();

                string studentEmail = submission.User?.Email ?? "";

                // 4. ВІДПРАВЛЯЄМО EMAIL
                if (!string.IsNullOrEmpty(studentEmail))
                {
                    string subject = $"Оцінка домашнього завдання: {submission.Lesson?.Title}";

                    string htmlMessage = $@"
                        <div style='font-family: Arial, sans-serif; padding: 20px; color: #333;'>
                            <h2 style='color: #0d6efd;'>Привіт, {submission.User?.UserName}!</h2>
                            <p>Ваш ментор перевірив домашнє завдання до уроку <strong>'{submission.Lesson?.Title}'</strong>.</p>
                            <div style='background-color: #f8f9fa; border-left: 4px solid #0d6efd; padding: 15px; margin: 20px 0;'>
                                <h4>Коментар ментора:</h4>
                                <p style='font-size: 16px; font-style: italic;'>{feedbackMessage}</p>
                            </div>
                            <p>Ви можете перевірити деталі в особистому кабінеті на сайті.</p>
                            <p>Продовжуйте в тому ж дусі!</p>
                            <p>З повагою,<br>Команда <b>EducationPlatform</b></p>
                        </div>";

                    await _emailSender.SendEmailAsync(studentEmail, subject, htmlMessage);
                }

                TempData["SuccessMessage"] = $"Відгук надіслано на пошту та у сповіщення студенту {submission.User?.UserName} 🚀";
            }

            return RedirectToAction("Homeworks");
        }

        [HttpPost]
        public async Task<IActionResult> SendNewsletter(string targetAudience, string subject, string message)
        {
            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(message))
            {
                TempData["ErrorMessage"] = "Тема та текст повідомлення не можуть бути порожніми.";
                return RedirectToAction("Index");
            }

            // ЗМІНА 1: Тепер збираємо об'єкти користувачів (щоб отримати їх Id для дзвіночка), а не просто тексти email-ів
            var targetUsers = new List<Microsoft.AspNetCore.Identity.IdentityUser>();

            if (targetAudience == "all")
            {
                targetUsers = await _context.Users.ToListAsync();
            }
            else if (targetAudience == "active")
            {
                targetUsers = await _context.Purchases
                    .Include(p => p.User)
                    .Select(p => p.User)
                    .Distinct()
                    .ToListAsync();
            }
            else if (targetAudience.StartsWith("course_"))
            {
                if (int.TryParse(targetAudience.Replace("course_", ""), out int courseId))
                {
                    targetUsers = await _context.Purchases
                        .Where(p => p.CourseId == courseId)
                        .Include(p => p.User)
                        .Select(p => p.User)
                        .Distinct()
                        .ToListAsync();
                }
            }

            // Очищаємо від користувачів без пошти
            targetUsers = targetUsers.Where(u => u != null && !string.IsNullOrWhiteSpace(u.Email)).ToList();

            if (!targetUsers.Any())
            {
                TempData["ErrorMessage"] = "Не знайдено жодного отримувача для цієї категорії!";
                return RedirectToAction("Index");
            }

            int successCount = 0;
            int errorCount = 0;
            string lastError = "";

            foreach (var user in targetUsers)
            {
                try
                {
                    // 1. ВІДПРАВЛЯЄМО EMAIL
                    string htmlMessage = $@"
                        <div style='font-family: Arial, sans-serif; padding: 20px; color: #333; background-color: #f4f4f4;'>
                            <div style='background-color: #ffffff; padding: 20px; border-radius: 8px; max-width: 600px; margin: auto;'>
                                <h2 style='color: #0d6efd;'>Сповіщення від ITskill</h2>
                                <p style='font-size: 16px; line-height: 1.6;'>{message.Replace("\n", "<br>")}</p>
                                <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;' />
                                <p style='font-size: 12px; color: #888; text-align: center;'>Ви отримали цей лист, бо є студентом ITskill.</p>
                            </div>
                        </div>";

                    await _emailSender.SendEmailAsync(user.Email, subject, htmlMessage);

                    // =======================================================
                    // ЗМІНА 2: СТВОРЮЄМО СПОВІЩЕННЯ НА САЙТІ (ДЛЯ ДЗВІНОЧКА)
                    // =======================================================
                    var notification = new Notification
                    {
                        UserId = user.Id, // Беремо Id цього конкретного отримувача
                        Message = $"Важливе повідомлення: {subject}",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Notifications.Add(notification);

                    successCount++;

                    // Пауза, щоб Google не заблокував за спам
                    await Task.Delay(1000);
                }
                catch (Exception ex)
                {
                    errorCount++;
                    lastError = ex.Message;
                }
            }

            // =======================================================
            // ЗМІНА 3: ЗБЕРІГАЄМО СПОВІЩЕННЯ В БАЗУ
            // =======================================================
            await _context.SaveChangesAsync();

            if (errorCount == 0)
            {
                TempData["SuccessMessage"] = $"Розсилку успішно відправлено та створено сповіщення для {successCount} студентів! 🚀";
            }
            else
            {
                TempData["ErrorMessage"] = $"Відправлено: {successCount}. Помилок: {errorCount}. Остання помилка: {lastError}";
            }

            return RedirectToAction("Index");
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