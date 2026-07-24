using System;
using System.Collections.Generic;

namespace FptuGradingSystem.Domain.Entities
{
    public class Grade
    {
        public int Id { get; set; }
        public int SubmissionId { get; set; }
        public int GradedById { get; set; }
        public decimal TotalScore { get; set; }
        public string GeneralFeedback { get; set; } = string.Empty;
        public DateTime GradedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Submission? Submission { get; set; }
        public virtual User? GradedBy { get; set; }
        public virtual ICollection<GradeDetail> GradeDetails { get; set; } = new List<GradeDetail>();
    }
}
