using System.Collections.Generic;

namespace FptuGradingSystem.Domain.Entities
{
    public class Submission
    {
        public int Id { get; set; }
        public int ExamClassId { get; set; }
        public string StudentId { get; set; } = string.Empty; // e.g. SE150123
        public string StudentName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty; // Student email for notifications
        public string FilePath { get; set; } = string.Empty; // Path on disk
        public string FileType { get; set; } = string.Empty; // docx, xlsx, pdf
        public string Status { get; set; } = "Unassigned"; // Unassigned, Draft, Graded, Absent, Unrecognized

        // Navigation properties
        public virtual ExamClass? ExamClass { get; set; }
        public virtual Grade? Grade { get; set; }
        public virtual ICollection<GradeAppeal> Appeals { get; set; } = new List<GradeAppeal>();
        public virtual ICollection<GradeAuditLog> AuditLogs { get; set; } = new List<GradeAuditLog>();
    }
}

