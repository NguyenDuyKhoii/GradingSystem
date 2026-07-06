using System.Collections.Generic;

namespace FptuGradingSystem.Domain.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // Lecturer / AcademicStaff
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;

        // Navigation properties
        public virtual ICollection<ExamClass> ExamClasses { get; set; } = new List<ExamClass>();
        public virtual ICollection<Grade> Grades { get; set; } = new List<Grade>();
    }
}
