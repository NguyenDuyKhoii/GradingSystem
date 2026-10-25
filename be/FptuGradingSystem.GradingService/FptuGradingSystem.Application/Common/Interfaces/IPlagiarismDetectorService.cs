using System.Collections.Generic;

namespace FptuGradingSystem.Application.Common.Interfaces
{
    public record SubmissionCodeContent(
        int SubmissionId,
        string StudentId,
        string StudentName,
        string Content);

    public record SimilarityComparisonResult(
        int SubmissionAId,
        string StudentAId,
        string StudentAName,
        int SubmissionBId,
        string StudentBId,
        string StudentBName,
        decimal SimilarityPercentage,
        string Details);

    public interface IPlagiarismDetectorService
    {
        List<SimilarityComparisonResult> AnalyzeSubmissions(
            List<SubmissionCodeContent> submissions,
            decimal thresholdPercentage);
    }
}
