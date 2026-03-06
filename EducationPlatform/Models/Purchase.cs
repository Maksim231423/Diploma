using Microsoft.AspNetCore.Identity;

namespace EducationPlatform.Models
{
    public class Purchase
    {
        public int Id { get; set; }

        // Зв'язок з користувачем (Identity)
        public string UserId { get; set; }
        public IdentityUser User { get; set; }

        // Зв'язок з курсом
        public int CourseId { get; set; }
        public Course Course { get; set; }

        public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
    }
}
