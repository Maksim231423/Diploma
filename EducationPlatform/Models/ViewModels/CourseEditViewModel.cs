using System.Collections.Generic;

namespace EducationPlatform.Models.ViewModels
{
    public class CourseEditViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string ImageUrl { get; set; }
        public List<int> SelectedTags { get; set; } = new();

        // ЗМІНЕНО: Тепер ми використовуємо спеціальну DTO модель для уроків
        public List<LessonViewModel> Lessons { get; set; } = new();
    }

    // ДОДАНО: Спеціальний клас для збору даних з таблиці уроків
    public class LessonViewModel
    {
        public int Id { get; set; } // Якщо 0 - значить це новий урок
        public string Title { get; set; }
        public int OrderNumber { get; set; }
        public string VideoUrl { get; set; }
        public string HomeworkDescription { get; set; }
    }
}