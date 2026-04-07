using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

public class EmailModel : PageModel
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly IEmailSender _emailSender;

    public EmailModel(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, IEmailSender emailSender)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailSender = emailSender;
    }

    public string Email { get; set; }
    [BindProperty]
    public InputModel Input { get; set; }
    [TempData]
    public string StatusMessage { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Введіть новий Email")]
        [EmailAddress(ErrorMessage = "Некоректний формат пошти")]
        public string NewEmail { get; set; }

        public string VerificationCode { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        Email = await _userManager.GetEmailAsync(user);
        return Page();
    }

    // ХЕНДЛЕР: Відправка 6-значного коду
    public async Task<IActionResult> OnPostSendVerificationEmailAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        // 1. ПЕРЕВІРКА: чи не збігається нова пошта зі старою
        string currentEmail = await _userManager.GetEmailAsync(user);
        if (Input.NewEmail == currentEmail)
        {
            ModelState.AddModelError("Input.NewEmail", "Нова пошта не може бути такою ж, як теперішня.");
            return Page(); // Повертаємо ту саму сторінку з помилкою
        }

        // 2. Геруємо код
        Random generator = new Random();
        string code = generator.Next(100000, 999999).ToString();

        // Зберігаємо в TempData
        TempData["EmailUpdateCode"] = code;
        TempData["PendingNewEmail"] = Input.NewEmail;

        // 3. Відправляємо лист
        await _emailSender.SendEmailAsync(currentEmail, "Підтвердження зміни Email",
            $"Ваш код підтвердження: <b style='font-size:24px;'>{code}</b>. Якщо ви цього не робили - змініть пароль!");

        StatusMessage = "Код відправлено на вашу теперішню пошту.";

        // ВАЖЛИВО: Замість RedirectToPage() просто повертаємо Page()
        // Це дозволить зберегти введене значення в полі Input.NewEmail
        Email = currentEmail;
        return Page();
    }

    // 2. ХЕНДЛЕР: Перевірка коду та фінальна зміна Email
    public async Task<IActionResult> OnPostChangeEmailAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        // Дістаємо дані, які ми поклали в TempData під час відправки коду
        var savedCode = TempData["EmailUpdateCode"] as string;
        var newEmail = TempData["PendingNewEmail"] as string;

        // Щоб TempData не видалилося після першого ж читання (якщо код буде невірний),
        // ми можемо його "притримати" для наступної спроби
        TempData.Keep("EmailUpdateCode");
        TempData.Keep("PendingNewEmail");

        // Перевірка коду
        if (string.IsNullOrEmpty(Input.VerificationCode) || Input.VerificationCode != savedCode)
        {
            ModelState.AddModelError(string.Empty, "Невірний або прострочений код підтвердження.");
            return Page();
        }

        if (string.IsNullOrEmpty(newEmail))
        {
            ModelState.AddModelError(string.Empty, "Будь ласка, введіть новий Email ще раз.");
            return Page();
        }

        // Змінюємо Email
        var setEmailResult = await _userManager.SetEmailAsync(user, newEmail);

        if (!setEmailResult.Succeeded)
        {
            StatusMessage = "Помилка при зміні Email (можливо, така адреса вже зайнята).";
            return RedirectToPage();
        }

        // Оскільки у нас UserName базується на Email/Нікнеймі — оновлюємо і його
        // (Видали рядок нижче, якщо UserName у тебе — це постійний нікнейм, а не пошта)
        await _userManager.SetUserNameAsync(user, newEmail);

        await _signInManager.RefreshSignInAsync(user);
        StatusMessage = "Email успішно змінено на " + newEmail;

        return RedirectToPage();
    }
}