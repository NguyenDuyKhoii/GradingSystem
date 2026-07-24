using System.Collections.Generic;

namespace FptuGradingSystem.Domain.Entities
{
    public class Subject
    {
        public int Id { get; set; }
        public string SubjectCode { get; set; } = string.Empty; // e.g. PRN232
        public string SubjectName { get; set; } = string.Empty;

        // Navigation properties
        public virtual ICollection<Rubric> Rubrics { get; set; } = new List<Rubric>();
        public virtual ICollection<ExamClass> ExamClasses { get; set; } = new List<ExamClass>();
    }
}
