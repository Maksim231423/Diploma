using System;
using System.ComponentModel.DataAnnotations;

namespace EducationPlatform.Models
{
    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } // Зв'язок з користувачем (Identity)

        [Required]
        public string Message { get; set; } // Текст повідомлення, напр. "Доступний новий курс!"

        public bool IsRead { get; set; } = false; // Статус: прочитано чи ні (за замовчуванням false)

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Час створення
    }
}