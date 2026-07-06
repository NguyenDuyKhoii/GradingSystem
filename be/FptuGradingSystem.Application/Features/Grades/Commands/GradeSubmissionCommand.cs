using FptuGradingSystem.Application.Common.Interfaces;
using FptuGradingSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public GradeSubmissionCommandHandler(IApplicationDbContext context)
        {
            _context = context;
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

            // Verify lecturer
            var lecturer = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.GradedById, cancellationToken);
            if (lecturer == null || lecturer.Role != "Lecturer")
            {
                throw new UnauthorizedAccessException("Only lecturers can grade submissions.");
            }

            // Calculate total score based on weights
            decimal totalScore = 0;
            decimal totalWeight = rubric.Criteria.Sum(c => c.Weight);
            bool isPercentageWeight = Math.Abs(totalWeight - 100.0m) < 0.1m;

            var existingGrade = await _context.Grades
                .Include(g => g.GradeDetails)
                .FirstOrDefaultAsync(g => g.SubmissionId == request.SubmissionId, cancellationToken);

            // Create or update grade details
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

                // Add to total score calculation
                if (isPercentageWeight)
                {
                    totalScore += detailDto.Score * (criteria.Weight / 100m);
                }
                else
                {
                    totalScore += detailDto.Score * criteria.Weight;
                }

                details.Add(new GradeDetail
                {
                    RubricCriteriaId = detailDto.RubricCriteriaId,
                    Score = detailDto.Score,
                    Feedback = detailDto.Feedback
                });
            }

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

            return submission.Id;
        }
    }
}
