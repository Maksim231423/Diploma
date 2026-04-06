using Microsoft.AspNetCore.Identity;

namespace EducationPlatform.Data
{
    public static class DbInitializer
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

            // 1. Створюємо ТРИ ролі
            string[] roleNames = { "SuperAdmin", "Admin", "Student" };
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // 2. Створюємо Супер-Адміна
            var superAdminEmail = "boss@itskill.com";
            var superAdminPassword = "AdminPassword123!";

            var superAdminUser = await userManager.FindByEmailAsync(superAdminEmail);

            if (superAdminUser == null)
            {
                superAdminUser = new IdentityUser
                {
                    UserName = superAdminEmail,
                    Email = superAdminEmail,
                    EmailConfirmed = true
                };

                var createPowerUser = await userManager.CreateAsync(superAdminUser, superAdminPassword);

                if (createPowerUser.Succeeded)
                {
                    // Найвища роль
                    await userManager.AddToRoleAsync(superAdminUser, "SuperAdmin");
                }
            }
        }
    }
}