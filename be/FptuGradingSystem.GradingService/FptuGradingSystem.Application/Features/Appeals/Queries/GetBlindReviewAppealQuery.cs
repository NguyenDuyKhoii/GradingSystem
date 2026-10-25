using FptuGradingSystem.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.Application.Features.Appeals.Queries
{
    public record GetBlindReviewAppealQuery(int AppealId, int ReviewerLecturerId) : IRequest<BlindReviewAppealDto?>;

    public record BlindReviewAppealDto(
        int AppealId,
        int ExamClassId,
        int SubmissionId,
        string StudentId,
        string StudentName,
        string FilePath,
        string FileType,
        string AppealReason,
        string SubjectName,
        string SubjectCode,
        decimal? ReviewerCurrentTotalScore,
        string? ReviewerCurrentGeneralFeedback,
        List<BlindReviewCriteriaDto> Criteria);

    public record BlindReviewCriteriaDto(
        int RubricCriteriaId,
        string CriteriaName,
        string Description,
        decimal MaxPoints,
        decimal Weight,
        decimal? CurrentScore,
        string? CurrentFeedback);

    public class GetBlindReviewAppealQueryHandler : IRequestHandler<GetBlindReviewAppealQuery, BlindReviewAppealDto?>
    {
        private readonly IApplicationDbContext _context;

        public GetBlindReviewAppealQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<BlindReviewAppealDto?> Handle(GetBlindReviewAppealQuery request, CancellationToken cancellationToken)
        {
            var appeal = await _context.GradeAppeals
                .AsNoTracking()
                .Include(a => a.Submission)
                .Include(a => a.ReviewerDetails)
                .Include(a => a.ExamClass)
                    .ThenInclude(ec => ec!.Subject)
                        .ThenInclude(s => s!.Rubrics)
                            .ThenInclude(r => r.Criteria)
                .FirstOrDefaultAsync(a => a.Id == request.AppealId, cancellationToken);

            if (appeal == null) return null;

            // Security check: Only the assigned reviewer or AcademicStaff can view
            if (appeal.ReviewerLecturerId.HasValue && appeal.ReviewerLecturerId.Value != request.ReviewerLecturerId)
            {
                // Verify if user is AcademicStaff
                var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ReviewerLecturerId, cancellationToken);
                if (user == null || user.Role != "AcademicStaff")
                {
                    throw new System.UnauthorizedAccessException("Bạn không có quyền truy cập bài chấm phúc khảo này.");
                }
            }

            var rubric = appeal.ExamClass?.Subject?.Rubrics.FirstOrDefault();
            var reviewerDetailsMap = appeal.ReviewerDetails.ToDictionary(d => d.RubricCriteriaId);

            var criteriaDtos = rubric?.Criteria.Select(c =>
            {
                reviewerDetailsMap.TryGetValue(c.Id, out var revDetail);
                return new BlindReviewCriteriaDto(
                    c.Id,
                    c.CriteriaName,
                    c.Description,
                    c.MaxPoints,
                    c.Weight,
                    revDetail?.Score,
                    revDetail?.Feedback
                );
            }).ToList() ?? new List<BlindReviewCriteriaDto>();

            return new BlindReviewAppealDto(
                appeal.Id,
                appeal.ExamClassId,
                appeal.SubmissionId,
                appeal.StudentId,
                appeal.StudentName,
                appeal.Submission?.FilePath ?? string.Empty,
                appeal.Submission?.FileType ?? string.Empty,
                appeal.Reason,
                appeal.ExamClass?.Subject?.SubjectName ?? string.Empty,
                appeal.ExamClass?.Subject?.SubjectCode ?? string.Empty,
                appeal.ReviewerScore,
                appeal.ReviewerFeedback,
                criteriaDtos
            );
        }
    }
}
