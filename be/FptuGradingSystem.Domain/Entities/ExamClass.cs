using System.Collections.Generic;

namespace FptuGradingSystem.Domain.Entities
{
    public class ExamClass
    {
        public int Id { get; set; }
        public int ClassId { get; set; }
        public int SubjectId { get; set; }
        public string Semester { get; set; } = string.Empty; // e.g. SU26
        public int? LecturerId { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Grading, Completed

        // Navigation properties
        public virtual Class? Class { get; set; }
        public virtual Subject? Subject { get; set; }
        public virtual User? Lecturer { get; set; }
        public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();
    }
}
