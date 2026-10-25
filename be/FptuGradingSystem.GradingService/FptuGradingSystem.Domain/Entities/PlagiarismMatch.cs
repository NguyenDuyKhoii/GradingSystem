namespace FptuGradingSystem.Domain.Entities
{
    public class PlagiarismMatch
    {
        public int Id { get; set; }
        public int ReportId { get; set; }

        public int SubmissionAId { get; set; }
        public string StudentAId { get; set; } = string.Empty;
        public string StudentAName { get; set; } = string.Empty;

        public int SubmissionBId { get; set; }
        public string StudentBId { get; set; } = string.Empty;
        public string StudentBName { get; set; } = string.Empty;

        public decimal SimilarityPercentage { get; set; }
        public string Details { get; set; } = string.Empty;

        public virtual PlagiarismReport? Report { get; set; }
    }
}
