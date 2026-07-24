using FptuGradingSystem.Application.Common.Interfaces;
using FptuGradingSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.Application.Features.Grades.Commands
{
    public record GradeSubmissionCommand(
        int SubmissionId,
        int GradedById,
        string GeneralFeedback,
        bool IsDraft,
        List<CreateGradeDetailDto> Details) : IRequest<int>;

    public record CreateGradeDetailDto(
        int RubricCriteriaId,
        decimal Score,
        string Feedback);

    public class GradeSubmissionCommandHandler : IRequestHandler<GradeSubmissionCommand, int>
    {
        private readonly IApplicationDbContext _context;
        private readonly IGradingGrpcClient _gradingClient;
        private readonly IMessagePublisher _messagePublisher;

        public GradeSubmissionCommandHandler(
            IApplicationDbContext context,
            IGradingGrpcClient gradingClient,
            IMessagePublisher messagePublisher)
        {
            _context = context;
            _gradingClient = gradingClient;
            _messagePublisher = messagePublisher;
        }

        public async Task<int> Handle(GradeSubmissionCommand request, CancellationToken cancellationToken)
        {
            var submission = await _context.Submissions
                .Include(s => s.ExamClass)
                .ThenInclude(ec => ec!.Subject)
                .ThenInclude(sub => sub!.Rubrics)
                .ThenInclude(r => r.Criteria)
                .FirstOrDefaultAsync(s => s.Id == request.SubmissionId, cancellationToken);

            if (submission == null)
            {
                throw new KeyNotFoundException($"Submission with ID {request.SubmissionId} not found.");
            }

            var examClass = submission.ExamClass;
            var rubric = examClass?.Subject?.Rubrics.FirstOrDefault();
            if (rubric == null)
            {
                throw new InvalidOperationException("No Rubric defined for this subject.");
            }

            // GradedById is verified via JWT token in controller


            // Validate criteria scores and prepare for gRPC call
            decimal totalWeight = rubric.Criteria.Sum(c => c.Weight);
            bool isPercentageWeight = Math.Abs(totalWeight - 100.0m) < 0.1m;

            var criteriaScores = new List<CriteriaScoreDto>();
            var details = new List<GradeDetail>();

            foreach (var detailDto in request.Details)
            {
                var criteria = rubric.Criteria.FirstOrDefault(c => c.Id == detailDto.RubricCriteriaId);
                if (criteria == null)
                {
                    throw new ArgumentException($"Invalid Criteria ID {detailDto.RubricCriteriaId} for this rubric.");
                }

                if (detailDto.Score > criteria.MaxPoints)
                {
                    throw new ArgumentException($"Score for {criteria.CriteriaName} ({detailDto.Score}) exceeds Max Points ({criteria.MaxPoints}).");
                }

                // Prepare criteria data for gRPC calculation
                criteriaScores.Add(new CriteriaScoreDto(
                    criteria.Id, detailDto.Score, criteria.MaxPoints, criteria.Weight));

                details.Add(new GradeDetail
                {
                    RubricCriteriaId = detailDto.RubricCriteriaId,
                    Score = detailDto.Score,
                    Feedback = detailDto.Feedback
                });
            }

            // === gRPC Call: Calculate total score via GradingCalculator service ===
            var gradingResult = await _gradingClient.CalculateTotalScoreAsync(
                criteriaScores, isPercentageWeight);

            decimal totalScore = gradingResult.TotalScore;

            var existingGrade = await _context.Grades
                .Include(g => g.GradeDetails)
                .FirstOrDefaultAsync(g => g.SubmissionId == request.SubmissionId, cancellationToken);

            if (existingGrade != null)
            {
                // Update
                existingGrade.GradedById = request.GradedById;
                existingGrade.TotalScore = Math.Round(totalScore, 2);
                existingGrade.GeneralFeedback = request.GeneralFeedback;
                existingGrade.GradedAt = DateTime.UtcNow;

                // Simple replace details
                _context.GradeDetails.RemoveRange(existingGrade.GradeDetails);
                existingGrade.GradeDetails = details;
            }
            else
            {
                // Create
                var newGrade = new Grade
                {
                    SubmissionId = request.SubmissionId,
                    GradedById = request.GradedById,
                    TotalScore = Math.Round(totalScore, 2),
                    GeneralFeedback = request.GeneralFeedback,
                    GradedAt = DateTime.UtcNow,
                    GradeDetails = details
                };
                _context.Grades.Add(newGrade);
            }

            // Update submission status
            submission.Status = request.IsDraft ? "Draft" : "Graded";

            // If all submissions in the class are graded, we could update ExamClass status to "Graded"
            await _context.SaveChangesAsync(cancellationToken);

            // === Message Broker: Publish grade event to Redis ===
            if (!request.IsDraft)
            {
                var gradeEvent = new
                {
                    SubmissionId = submission.Id,
                    StudentId = submission.StudentId,
                    StudentName = submission.StudentName,
                    TotalScore = totalScore,
                    LetterGrade = gradingResult.LetterGrade,
                    IsPassed = gradingResult.IsPassed,
                    GradedBy = $"Lecturer #{request.GradedById}",
                    GradedAt = DateTime.UtcNow
                };

                await _messagePublisher.PublishAsync(
                    "grade:submitted",
                    JsonSerializer.Serialize(gradeEvent));
            }

            return submission.Id;
        }
    }
}
