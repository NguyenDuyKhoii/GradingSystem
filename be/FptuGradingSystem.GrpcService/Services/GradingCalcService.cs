using Grpc.Core;
using FptuGradingSystem.Grpc;

namespace FptuGradingSystem.GrpcService.Services;

public class GradingCalcService : GradingCalculator.GradingCalculatorBase
{
    private readonly ILogger<GradingCalcService> _logger;

    public GradingCalcService(ILogger<GradingCalcService> logger)
    {
        _logger = logger;
    }

    public override Task<CalculateScoreReply> CalculateTotalScore(
        CalculateScoreRequest request, ServerCallContext context)
    {
        _logger.LogInformation("gRPC CalculateTotalScore called with {Count} criteria", 
            request.CriteriaScores.Count);

        double totalScore = 0;

        foreach (var criteria in request.CriteriaScores)
        {
            if (criteria.MaxPoints <= 0)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument,
                    $"MaxPoints must be positive for criteria {criteria.CriteriaId}"));
            }

            if (criteria.Score < 0 || criteria.Score > criteria.MaxPoints)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument,
                    $"Score {criteria.Score} is out of range [0, {criteria.MaxPoints}] for criteria {criteria.CriteriaId}"));
            }

            if (request.IsPercentageWeight)
            {
                // Weight is percentage (e.g., 25 means 25%)
                totalScore += criteria.Score * (criteria.Weight / 100.0);
            }
            else
            {
                // Weight is decimal (e.g., 0.25)
                totalScore += criteria.Score * criteria.Weight;
            }
        }

        totalScore = Math.Round(totalScore, 2);

        string letterGrade = totalScore switch
        {
            >= 9.0 => "A+",
            >= 8.5 => "A",
            >= 8.0 => "B+",
            >= 7.0 => "B",
            >= 6.5 => "C+",
            >= 5.5 => "C",
            >= 5.0 => "D+",
            >= 4.0 => "D",
            _ => "F"
        };

        bool isPassed = totalScore >= 4.0;

        _logger.LogInformation(
            "Calculated TotalScore: {TotalScore}, LetterGrade: {LetterGrade}, Passed: {IsPassed}",
            totalScore, letterGrade, isPassed);

        return Task.FromResult(new CalculateScoreReply
        {
            TotalScore = totalScore,
            LetterGrade = letterGrade,
            IsPassed = isPassed
        });
    }
}
