using System;

namespace FptuGradingSystem.Domain.Entities
{
    public class GradeAuditLog
    {
        public int Id { get; set; }
        public int SubmissionId { get; set; }
        public int? GradeId { get; set; }
        public int? RubricCriteriaId { get; set; }
        public string CriteriaName { get; set; } = string.Empty;
        public decimal OldScore { get; set; }
        public decimal NewScore { get; set; }
        public string? OldFeedback { get; set; }
        public string? NewFeedback { get; set; }
        public int ChangedById { get; set; }
        public string ChangedByName { get; set; } = string.Empty;
        public string ChangedByRole { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Submission? Submission { get; set; }
        public virtual Grade? Grade { get; set; }
        public virtual RubricCriteria? RubricCriteria { get; set; }
        public virtual User? ChangedBy { get; set; }
    }
}
