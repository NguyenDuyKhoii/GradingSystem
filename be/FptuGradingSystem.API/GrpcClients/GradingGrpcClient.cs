using FptuGradingSystem.Application.Common.Interfaces;
using FptuGradingSystem.Grpc;
using Grpc.Net.Client;

namespace FptuGradingSystem.API.GrpcClients
{
    public class GradingGrpcClient : IGradingGrpcClient
    {
        private readonly GradingCalculator.GradingCalculatorClient _client;
        private readonly ILogger<GradingGrpcClient> _logger;

        public GradingGrpcClient(GrpcChannel channel, ILogger<GradingGrpcClient> logger)
        {
            _client = new GradingCalculator.GradingCalculatorClient(channel);
            _logger = logger;
        }

        public async Task<GradingResultDto> CalculateTotalScoreAsync(
            List<CriteriaScoreDto> criteriaScores, bool isPercentageWeight)
        {
            _logger.LogInformation("Calling gRPC GradingCalculator.CalculateTotalScore with {Count} criteria",
                criteriaScores.Count);

            var request = new CalculateScoreRequest
            {
                IsPercentageWeight = isPercentageWeight
            };

            foreach (var cs in criteriaScores)
            {
                request.CriteriaScores.Add(new CriteriaScore
                {
                    CriteriaId = cs.CriteriaId,
                    Score = (double)cs.Score,
                    MaxPoints = (double)cs.MaxPoints,
                    Weight = (double)cs.Weight
                });
            }

            var reply = await _client.CalculateTotalScoreAsync(request);

            _logger.LogInformation(
                "gRPC response: TotalScore={TotalScore}, LetterGrade={LetterGrade}, Passed={IsPassed}",
                reply.TotalScore, reply.LetterGrade, reply.IsPassed);

            return new GradingResultDto(
                (decimal)reply.TotalScore,
                reply.LetterGrade,
                reply.IsPassed);
        }
    }
}
