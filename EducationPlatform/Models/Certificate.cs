using Microsoft.AspNetCore.Identity;
using System;

namespace EducationPlatform.Models
{
    public class Certificate
    {
        public int Id { get; set; }

        public string UserId { get; set; }
        public IdentityUser User { get; set; }

        public int CourseId { get; set; }
        public Course Course { get; set; }

        public DateTime IssueDate { get; set; }

        //"криптографічне посилання" (унікальний токен)
        public string UniqueToken { get; set; }

        public string StudentFirstName { get; set; }
        public string StudentLastName { get; set; }
    }
}