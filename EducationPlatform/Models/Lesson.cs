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

        [Display(Name = "Посилання на Урок")]
        public string VideoUrl { get; set; }

        public int OrderNumber { get; set; } // Порядковий номер (1, 2, 3...)

        [Display(Name = "Опис домашнього завдання")]
        public string? HomeworkDescription { get; set; }
        // Знак питання (?) означає, що поле не обов'язкове (адже не в кожному уроці може бути домашка)

        [Display(Name = "Що ви навчитеся (кожен пункт з нового рядка)")]
        public string? LearningObjectives { get; set; }

        // Зв'язок з курсом
        public int CourseId { get; set; }
        public Course Course { get; set; }
    }
}