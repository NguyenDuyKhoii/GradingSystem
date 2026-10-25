using System;
using System.Collections.Generic;

namespace FptuGradingSystem.Domain.Entities
{
    public class RosterReconciliationReport
    {
        public int Id { get; set; }
        public int ExamClassId { get; set; }
        public DateTime ReconciledAt { get; set; } = DateTime.UtcNow;
        public int TotalInRoster { get; set; }
        public int TotalSubmissions { get; set; }
        public int MatchedCount { get; set; }
        public int AbsentCount { get; set; }
        public int UnrecognizedCount { get; set; }

        public virtual ExamClass? ExamClass { get; set; }
        public virtual ICollection<RosterReconciliationItem> Items { get; set; } = new List<RosterReconciliationItem>();
    }
}
