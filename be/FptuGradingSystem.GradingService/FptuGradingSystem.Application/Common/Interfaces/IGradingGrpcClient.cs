namespace FptuGradingSystem.Application.Common.Interfaces
{
    public interface IGradingGrpcClient
    {
        Task<GradingResultDto> CalculateTotalScoreAsync(
            List<CriteriaScoreDto> criteriaScores, bool isPercentageWeight);
    }

    public record CriteriaScoreDto(int CriteriaId, decimal Score, decimal MaxPoints, decimal Weight);
    public record GradingResultDto(decimal TotalScore, string LetterGrade, bool IsPassed);
}
