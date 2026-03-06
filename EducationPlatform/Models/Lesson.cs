using System.ComponentModel.DataAnnotations;

namespace EducationPlatform.Models
{
    public class Lesson
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Тема уроку")]
        public string Title { get; set; }

        [Display(Name = "Зміст")]
        public string Content { get; set; }

        [Display(Name = "Посилання на YouTube")]
        public string VideoUrl { get; set; }

        public int OrderNumber { get; set; } // Порядковий номер (1, 2, 3...)

        // Зв'язок з курсом
        public int CourseId { get; set; }
        public Course Course { get; set; }
    }
}