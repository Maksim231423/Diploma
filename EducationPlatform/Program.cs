using EducationPlatform.Data;
using EducationPlatform.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

namespace EducationPlatform
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0))));
            object value = builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddDefaultIdentity<IdentityUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = true; // Обов'язкове підтвердження для входу
                // ДОЗВОЛЯЄМО УКРАЇНСЬКУ МОВУ ТА ПРОБІЛИ В ІМЕНІ
                options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_";
            })
                .AddRoles<IdentityRole>() // Важливо для роботи ролей
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders() // <--- Цей метод сам додає всі потрібні генератори кодів (включно з Email)
                .AddErrorDescriber<EducationPlatform.Services.CustomIdentityErrorDescriber>();
            builder.Services.AddControllersWithViews();

            //Тепер є сервіс для відправкли листа, який використовує клас EmailSender
            builder.Services.AddTransient<IEmailSender, EmailSender>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            // 1. ������� ��� ��̲��� (�� ���� ������!)
            app.MapControllerRoute(
                name: "areas",
                pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

            // 2. ������� ��� ���������� ����� (�� ���� ������)
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.MapRazorPages(); // �� ��� Identity (Login/Register), �� �������


            // Автоматичне створення ролей та Адміна при запуску
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    await EducationPlatform.Data.DbInitializer.SeedRolesAndAdminAsync(services);
                }
                catch (Exception ex)
                {
                    // Якщо раптом щось піде не так (наприклад, немає підключення до БД), помилка виведеться в консоль
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "Помилка під час ініціалізації ролей у базі даних.");
                }
            }

            app.Run();
        }
    }
}
