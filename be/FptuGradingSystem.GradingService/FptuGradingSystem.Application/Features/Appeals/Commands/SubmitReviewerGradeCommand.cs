using FptuGradingSystem.Application.Common.Interfaces;
using FptuGradingSystem.Application.Features.Grades.Commands;
using FptuGradingSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.Application.Features.Appeals.Commands
{
    public record SubmitReviewerGradeCommand(
        int AppealId,
        int ReviewerLecturerId,
        string ReviewerFeedback,
        List<CreateGradeDetailDto> Details) : IRequest<GradeAppealDto>;

    public class SubmitReviewerGradeCommandHandler : IRequestHandler<SubmitReviewerGradeCommand, GradeAppealDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IGradingGrpcClient _gradingClient;

        public SubmitReviewerGradeCommandHandler(
            IApplicationDbContext context,
            IGradingGrpcClient gradingClient)
        {
            _context = context;
            _gradingClient = gradingClient;
        }

        public async Task<GradeAppealDto> Handle(SubmitReviewerGradeCommand request, CancellationToken cancellationToken)
        {
            var appeal = await _context.GradeAppeals
                .Include(a => a.ReviewerDetails)
                    .ThenInclude(d => d.RubricCriteria)
                .Include(a => a.ReviewerLecturer)
                .Include(a => a.ResolvedBy)
                .Include(a => a.ExamClass)
                    .ThenInclude(ec => ec!.Subject)
                        .ThenInclude(s => s!.Rubrics)
                            .ThenInclude(r => r.Criteria)
                .FirstOrDefaultAsync(a => a.Id == request.AppealId, cancellationToken);

            if (appeal == null)
            {
                throw new KeyNotFoundException($"Đơn phúc khảo #{request.AppealId} không tồn tại.");
            }

            if (appeal.Status != "Assigned" && appeal.Status != "ReviewCompleted")
            {
                throw new InvalidOperationException($"Không thể chấm phúc khảo cho đơn ở trạng thái '{appeal.Status}'.");
            }

            if (appeal.ReviewerLecturerId != request.ReviewerLecturerId)
            {
                throw new UnauthorizedAccessException("Bạn không phải là Giảng viên được phân công chấm phúc khảo cho bài thi này.");
            }

            var rubric = appeal.ExamClass?.Subject?.Rubrics.FirstOrDefault();
            if (rubric == null)
            {
                throw new InvalidOperationException("Môn học chưa có Rubric chấm điểm.");
            }

            decimal totalWeight = rubric.Criteria.Sum(c => c.Weight);
            bool isPercentageWeight = Math.Abs(totalWeight - 100.0m) < 0.1m;

            var criteriaScores = new List<CriteriaScoreDto>();
            var newDetails = new List<GradeAppealDetail>();

            foreach (var detailDto in request.Details)
            {
                var criteria = rubric.Criteria.FirstOrDefault(c => c.Id == detailDto.RubricCriteriaId);
                if (criteria == null)
                {
                    throw new ArgumentException($"Mã tiêu chí #{detailDto.RubricCriteriaId} không hợp lệ trong rubric môn học.");
                }

                if (detailDto.Score > criteria.MaxPoints)
                {
                    throw new ArgumentException($"Điểm cho tiêu chí '{criteria.CriteriaName}' ({detailDto.Score}) vượt quá điểm tối đa ({criteria.MaxPoints}).");
                }

                criteriaScores.Add(new CriteriaScoreDto(
                    criteria.Id, detailDto.Score, criteria.MaxPoints, criteria.Weight));

                newDetails.Add(new GradeAppealDetail
                {
                    GradeAppealId = appeal.Id,
                    RubricCriteriaId = detailDto.RubricCriteriaId,
                    Score = detailDto.Score,
                    Feedback = detailDto.Feedback
                });
            }

            // Calculate total score using gRPC service
            var gradingResult = await _gradingClient.CalculateTotalScoreAsync(criteriaScores, isPercentageWeight);
            decimal totalScore = Math.Round(gradingResult.TotalScore, 2);

            _context.GradeAppealDetails.RemoveRange(appeal.ReviewerDetails);
            appeal.ReviewerDetails = newDetails;
            appeal.ReviewerScore = totalScore;
            appeal.ReviewerFeedback = request.ReviewerFeedback;
            appeal.ReviewedAt = DateTime.UtcNow;
            appeal.Status = "ReviewCompleted";

            await _context.SaveChangesAsync(cancellationToken);

            return new GradeAppealDto(
                appeal.Id,
                appeal.ExamClassId,
                appeal.SubmissionId,
                appeal.StudentId,
                appeal.StudentName,
                appeal.StudentEmail,
                appeal.Reason,
                appeal.RequestedAt,
                appeal.Status,
                appeal.OriginalScore,
                appeal.OriginalGradedById,
                appeal.ReviewerLecturerId,
                appeal.ReviewerLecturer?.FullName,
                appeal.ReviewerScore,
                appeal.ReviewerFeedback,
                appeal.ReviewedAt,
                appeal.FinalScore,
                appeal.ResolvedAt,
                appeal.ResolvedById,
                appeal.ResolvedBy?.FullName,
                appeal.ResolutionNotes,
                appeal.ReviewerDetails.Select(d => new GradeAppealDetailDto(
                    d.Id,
                    d.RubricCriteriaId,
                    rubric.Criteria.FirstOrDefault(c => c.Id == d.RubricCriteriaId)?.CriteriaName ?? string.Empty,
                    rubric.Criteria.FirstOrDefault(c => c.Id == d.RubricCriteriaId)?.MaxPoints ?? 0,
                    d.Score,
                    d.Feedback
                )).ToList()
            );
        }
    }
}
