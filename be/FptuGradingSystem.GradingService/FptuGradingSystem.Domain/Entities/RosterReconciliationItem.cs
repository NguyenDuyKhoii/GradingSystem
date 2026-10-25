namespace FptuGradingSystem.Domain.Entities
{
    public class RosterReconciliationItem
    {
        public int Id { get; set; }
        public int ReportId { get; set; }

        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // Status: "Matched", "Absent", "Unrecognized"
        public string Status { get; set; } = "Matched";
        public int? SubmissionId { get; set; }
        public string Notes { get; set; } = string.Empty;

        public virtual RosterReconciliationReport? Report { get; set; }
    }
}
