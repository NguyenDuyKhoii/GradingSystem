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

        decimal totalScore = 0m;

        foreach (var criteria in request.CriteriaScores)
        {
            var score = criteria.Score.ToDecimal();
            var maxPoints = criteria.MaxPoints.ToDecimal();
            var weight = criteria.Weight.ToDecimal();

            if (maxPoints <= 0m)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument,
                    $"MaxPoints must be positive for criteria {criteria.CriteriaId}"));
            }

            if (score < 0m || score > maxPoints)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument,
                    $"Score {score} is out of range [0, {maxPoints}] for criteria {criteria.CriteriaId}"));
            }

            if (request.IsPercentageWeight)
            {
                // Weight is percentage (e.g., 25 means 25%)
                totalScore += score * (weight / 100.0m);
            }
            else
            {
                // Weight is decimal (e.g., 0.25)
                totalScore += score * weight;
            }
        }

        totalScore = Math.Round(totalScore, 2, MidpointRounding.AwayFromZero);

        string letterGrade = totalScore switch
        {
            >= 9.0m => "A+",
            >= 8.5m => "A",
            >= 8.0m => "B+",
            >= 7.0m => "B",
            >= 6.5m => "C+",
            >= 5.5m => "C",
            >= 5.0m => "D+",
            >= 4.0m => "D",
            _ => "F"
        };

        bool isPassed = totalScore >= 4.0m;

        _logger.LogInformation(
            "Calculated TotalScore: {TotalScore}, LetterGrade: {LetterGrade}, Passed: {IsPassed}",
            totalScore, letterGrade, isPassed);

        return Task.FromResult(new CalculateScoreReply
        {
            TotalScore = totalScore.ToDecimalValue(),
            LetterGrade = letterGrade,
            IsPassed = isPassed
        });
    }
}
