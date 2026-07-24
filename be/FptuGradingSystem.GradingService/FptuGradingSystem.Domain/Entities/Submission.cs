namespace FptuGradingSystem.Domain.Entities
{
    public class Submission
    {
        public int Id { get; set; }
        public int ExamClassId { get; set; }
        public string StudentId { get; set; } = string.Empty; // e.g. SE150123
        public string StudentName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty; // Path on disk
        public string FileType { get; set; } = string.Empty; // docx, xlsx, pdf
        public string Status { get; set; } = "Unassigned"; // Unassigned, Draft, Graded

        // Navigation properties
        public virtual ExamClass? ExamClass { get; set; }
        public virtual Grade? Grade { get; set; }
    }
}
