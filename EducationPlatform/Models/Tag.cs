namespace EducationPlatform.Models
{
    public class Tag
    {
        public int Id { get; set; }

        // Назва тегу, наприклад "C#", "Бази Даних"
        public string Name { get; set; }

        // Навігаційна властивість: один тег може належати багатьом курсам
        public List<Course> Courses { get; set; } = new List<Course>();
    }
}