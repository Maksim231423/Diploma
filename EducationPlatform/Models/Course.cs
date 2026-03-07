using System.ComponentModel.DataAnnotations;

namespace EducationPlatform.Models
{
    public class Course
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Введіть назву курсу")]
        [Display(Name = "Назва курсу")]
        public string Title { get; set; }

        [Display(Name = "Опис")]
        public string Description { get; set; }

        [Display(Name = "Ціна")]
        public decimal Price { get; set; }

        [Display(Name = "Картинка курсу")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Дата створення")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<Lesson> Lessons { get; set; } = new List<Lesson>();

        public List<Comment> Comments { get; set; } = new List<Comment>();

        // Навігаційна властивість: один курс може мати багато тегів
        public List<Tag> Tags { get; set; } = new List<Tag>();
    }
}