using System.Collections.Generic;

namespace EducationPlatform.Models.ViewModels
{
    public class UserRoleViewModel
    {
        public string UserId { get; set; }
        public string Email { get; set; }
        public IList<string> Roles { get; set; } // Список ролей, які має цей користувач
    }
}