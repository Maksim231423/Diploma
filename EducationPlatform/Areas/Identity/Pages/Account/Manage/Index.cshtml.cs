using EducationPlatform.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace EducationPlatform.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ApplicationDbContext _context; // ДОДАЄМО БАЗУ ДАНИХ

        public IndexModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        // Список для виводу курсів на екран
        public List<CourseProgressViewModel> MyCourses { get; set; } = new List<CourseProgressViewModel>();

        public class InputModel
        {
            [Required(ErrorMessage = "Ім'я користувача не може бути порожнім")]
            [Display(Name = "Ім'я користувача")]
            public string Username { get; set; }
        }

        // Допоміжний клас для зберігання прогресу
        public class CourseProgressViewModel
        {
            public int CourseId { get; set; }
            public string Title { get; set; }
            public string ImageUrl { get; set; }
            public int TotalTasks { get; set; }
            public int SubmittedTasks { get; set; }
            public int CheckedTasks { get; set; }
            public int ProgressPercentage { get; set; }
        }

        private async Task LoadAsync(IdentityUser user)
        {
            Input = new InputModel
            {
                Username = await _userManager.GetUserNameAsync(user)
            };

            // ЗАВАНТАЖУЄМО КУРСИ ТА ДОМАШКИ
            var purchases = await _context.Purchases
                .Include(p => p.Course)
                .ThenInclude(c => c.Lessons)
                .Where(p => p.UserId == user.Id)
                .ToListAsync();

            var userSubmissions = await _context.HomeworkSubmissions
                .Where(h => h.UserId == user.Id)
                .ToListAsync();

            foreach (var purchase in purchases)
            {
                var course = purchase.Course;

                // Рахуємо скільки всього уроків мають домашку (!string.IsNullOrEmpty)
                int totalTasks = course.Lessons.Count(l => !string.IsNullOrEmpty(l.HomeworkDescription));

                // Отримуємо ID всіх уроків цього курсу
                var lessonIds = course.Lessons.Select(l => l.Id).ToList();

                // Рахуємо здані домашки саме для цього курсу
                var courseSubmissions = userSubmissions.Where(h => lessonIds.Contains(h.LessonId)).ToList();
                int submittedTasks = courseSubmissions.Count;
                int checkedTasks = courseSubmissions.Count(h => h.IsChecked);

                // Вираховуємо відсоток
                int percentage = totalTasks == 0 ? 0 : (int)Math.Round((double)submittedTasks / totalTasks * 100);

                MyCourses.Add(new CourseProgressViewModel
                {
                    CourseId = course.Id,
                    Title = course.Title,
                    ImageUrl = course.ImageUrl,
                    TotalTasks = totalTasks,
                    SubmittedTasks = submittedTasks,
                    CheckedTasks = checkedTasks,
                    ProgressPercentage = percentage
                });
            }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var currentUsername = await _userManager.GetUserNameAsync(user);
            if (Input.Username != currentUsername)
            {
                // ПЕРЕВІРКА НА УНІКАЛЬНІСТЬ ІМЕНІ
                var existingUser = await _userManager.FindByNameAsync(Input.Username);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Input.Username", "Це ім'я вже зайнято іншим користувачем.");
                    await LoadAsync(user);
                    return Page();
                }

                var setUserNameResult = await _userManager.SetUserNameAsync(user, Input.Username);
                if (!setUserNameResult.Succeeded)
                {
                    StatusMessage = "Помилка при спробі змінити ім'я.";
                    return RedirectToPage();
                }
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Ваш профіль успішно оновлено!";
            return RedirectToPage();
        }
    }
}