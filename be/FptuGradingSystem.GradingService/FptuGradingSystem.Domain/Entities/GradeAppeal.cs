using System;
using System.Collections.Generic;

namespace FptuGradingSystem.Domain.Entities
{
    public class GradeAppeal
    {
        public int Id { get; set; }
        public int ExamClassId { get; set; }
        public int SubmissionId { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Pending"; // Pending, Assigned, ReviewCompleted, Approved, Rejected

        // Grader 1 snapshot
        public decimal OriginalScore { get; set; }
        public int? OriginalGradedById { get; set; }

        // Reviewer (Grader 2) blind grading
        public int? ReviewerLecturerId { get; set; }
        public decimal? ReviewerScore { get; set; }
        public string? ReviewerFeedback { get; set; }
        public DateTime? ReviewedAt { get; set; }

        // Final Resolution
        public decimal? FinalScore { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public int? ResolvedById { get; set; }
        public string? ResolutionNotes { get; set; }

        // Navigation properties
        public virtual ExamClass? ExamClass { get; set; }
        public virtual Submission? Submission { get; set; }
        public virtual User? ReviewerLecturer { get; set; }
        public virtual User? ResolvedBy { get; set; }
        public virtual ICollection<GradeAppealDetail> ReviewerDetails { get; set; } = new List<GradeAppealDetail>();
    }
}
