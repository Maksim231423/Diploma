using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace EducationPlatform.Models
{
    public class HomeworkSubmission
    {
        public int Id { get; set; }

        // Зв'язок з уроком
        [Required]
        public int LessonId { get; set; }
        public Lesson Lesson { get; set; }

        // Зв'язок з користувачем (студентом)
        [Required]
        public string UserId { get; set; }
        public IdentityUser User { get; set; }

        // Саме посилання (на GitHub або Google Диск)
        [Required(ErrorMessage = "Введіть посилання на завдання")]
        [Url(ErrorMessage = "Введіть коректне посилання (наприклад, https://github.com/...)")]
        [Display(Name = "Посилання на рішення")]
        public string SolutionLink { get; set; }

        [Display(Name = "Дата відправки")]
        public DateTime SubmissionDate { get; set; } = DateTime.UtcNow;

        // Статус перевірки адміністратором
        [Display(Name = "Перевірено")]
        public bool IsChecked { get; set; } = false;
    }
}