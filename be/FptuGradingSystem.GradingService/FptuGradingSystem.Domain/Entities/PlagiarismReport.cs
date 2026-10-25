using System;
using System.Collections.Generic;

namespace FptuGradingSystem.Domain.Entities
{
    public class PlagiarismReport
    {
        public int Id { get; set; }
        public int ExamClassId { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public decimal ThresholdPercentage { get; set; } = 70.0m;
        public int TotalSubmissionsAnalyzed { get; set; }
        public int SuspectPairsCount { get; set; }

        public virtual ExamClass? ExamClass { get; set; }
        public virtual ICollection<PlagiarismMatch> Matches { get; set; } = new List<PlagiarismMatch>();
    }
}
