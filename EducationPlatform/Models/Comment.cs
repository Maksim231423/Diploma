using System.Data;
using Microsoft.AspNetCore.Identity;

namespace EducationPlatform.Models
{
    public class Comment
    {
        public int Id { get; set; }
        // Додане поле для імені, яке буде відображатися
        public string UserName { get; set; }
        public string Text { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Зв'язок з курсом
        public int CourseId { get; set; }
        public Course Course { get; set; }

        // Зв'язок з користувачем (автором)
        public string UserId { get; set; }
        public IdentityUser User { get; set; } // Використовуємо IdentityUser
    }
}
