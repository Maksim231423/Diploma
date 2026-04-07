using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

public class ChangePasswordModel : PageModel
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly IEmailSender _emailSender;

    public ChangePasswordModel(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, IEmailSender emailSender)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailSender = emailSender;
    }

    [BindProperty]
    public InputModel Input { get; set; }

    [TempData]
    public string StatusMessage { get; set; }

    public class InputModel
    {
        public string Email { get; set; }

        public string Code { get; set; }

        [Required(ErrorMessage = "Введіть новий пароль")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        Input = new InputModel { Email = await _userManager.GetEmailAsync(user) };
        return Page();
    }

    // ХЕНДЛЕР 1: Відправка коду (Аналог OnPostSendVerificationEmailAsync)
    public async Task<IActionResult> OnPostSendCodeAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        // 1. Генеруємо 6-значний код
        Random generator = new Random();
        string code = generator.Next(100000, 999999).ToString();

        // 2. Зберігаємо дані в TempData, як у файлі Email
        TempData["PasswordResetCode"] = code;
        TempData["PendingNewPassword"] = Input.NewPassword;

        // 3. Відправляємо лист на поточну пошту
        string email = await _userManager.GetEmailAsync(user);
        await _emailSender.SendEmailAsync(email, "Код для зміни пароля",
            $"Ваш код підтвердження: <b style='font-size:24px;'>{code}</b>. Якщо ви цього не робили - змініть пароль!");

        StatusMessage = "Код відправлено на вашу пошту.";

        // Повертаємо Page(), щоб зберегти введені дані в полях
        Input.Email = email;
        return Page();
    }

    // ХЕНДЛЕР 2: Сама зміна пароля (Аналог OnPostChangeEmailAsync)
    public async Task<IActionResult> OnPostChangePasswordAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        // Дістаємо дані з TempData
        var savedCode = TempData["PasswordResetCode"] as string;
        var pendingPassword = TempData["PendingNewPassword"] as string;

        // Підтримуємо дані в TempData (Keep), щоб при помилці вони не зникли
        TempData.Keep("PasswordResetCode");
        TempData.Keep("PendingNewPassword");

        // Перевірка коду
        if (string.IsNullOrEmpty(Input.Code) || Input.Code != savedCode)
        {
            ModelState.AddModelError("Input.Code", "Невірний або прострочений код підтвердження.");
            Input.Email = await _userManager.GetEmailAsync(user);
            return Page();
        }

        // Перевірка, чи не загубився пароль у TempData
        var passwordToSet = !string.IsNullOrEmpty(Input.NewPassword) ? Input.NewPassword : pendingPassword;

        if (string.IsNullOrEmpty(passwordToSet))
        {
            ModelState.AddModelError("Input.NewPassword", "Будь ласка, введіть новий пароль ще раз.");
            Input.Email = await _userManager.GetEmailAsync(user);
            return Page();
        }

        // Логіка зміни пароля
        var removeResult = await _userManager.RemovePasswordAsync(user);
        if (!removeResult.Succeeded)
        {
            StatusMessage = "Помилка при видаленні старого пароля.";
            return RedirectToPage();
        }

        var addResult = await _userManager.AddPasswordAsync(user, passwordToSet);
        if (!addResult.Succeeded)
        {
            foreach (var error in addResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            Input.Email = await _userManager.GetEmailAsync(user);
            return Page();
        }

        await _signInManager.RefreshSignInAsync(user);
        StatusMessage = "Пароль успішно змінено!";

        return RedirectToPage();
    }
}