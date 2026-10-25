using FptuGradingSystem.Application.Common.Interfaces;
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
    public record ResolveGradeAppealCommand(
        int AppealId,
        string Decision, // "Approved" or "Rejected"
        decimal? FinalScore,
        string ResolutionNotes,
        int ResolvedById) : IRequest<GradeAppealDto>;

    public class ResolveGradeAppealCommandHandler : IRequestHandler<ResolveGradeAppealCommand, GradeAppealDto>
    {
        private readonly IApplicationDbContext _context;

        public ResolveGradeAppealCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<GradeAppealDto> Handle(ResolveGradeAppealCommand request, CancellationToken cancellationToken)
        {
            var appeal = await _context.GradeAppeals
                .Include(a => a.ReviewerDetails)
                    .ThenInclude(d => d.RubricCriteria)
                .Include(a => a.ReviewerLecturer)
                .Include(a => a.ResolvedBy)
                .Include(a => a.Submission)
                    .ThenInclude(s => s!.Grade)
                        .ThenInclude(g => g!.GradeDetails)
                .FirstOrDefaultAsync(a => a.Id == request.AppealId, cancellationToken);

            if (appeal == null)
            {
                throw new KeyNotFoundException($"Đơn phúc khảo #{request.AppealId} không tồn tại.");
            }

            if (appeal.Status == "Approved" || appeal.Status == "Rejected")
            {
                throw new InvalidOperationException($"Đơn phúc khảo đã được giải quyết trước đó ({appeal.Status}).");
            }

            var resolver = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.ResolvedById, cancellationToken);
            if (resolver == null)
            {
                throw new KeyNotFoundException($"Người xử lý #{request.ResolvedById} không tồn tại.");
            }

            var submission = appeal.Submission;
            if (submission == null || submission.Grade == null)
            {
                throw new InvalidOperationException("Bài nộp hoặc bảng điểm gốc không tìm thấy.");
            }

            bool isApproved = request.Decision.Equals("Approved", StringComparison.OrdinalIgnoreCase);

            if (isApproved)
            {
                decimal resolvedScore = request.FinalScore ?? appeal.ReviewerScore ?? appeal.OriginalScore;
                decimal oldScore = submission.Grade.TotalScore;

                // Log audit for total score adjustment
                _context.GradeAuditLogs.Add(new GradeAuditLog
                {
                    SubmissionId = submission.Id,
                    GradeId = submission.Grade.Id,
                    CriteriaName = "Total Score (Grade Appeal)",
                    OldScore = oldScore,
                    NewScore = resolvedScore,
                    OldFeedback = submission.Grade.GeneralFeedback,
                    NewFeedback = $"{submission.Grade.GeneralFeedback} | [Phúc khảo]: {request.ResolutionNotes}",
                    ChangedById = request.ResolvedById,
                    ChangedByName = resolver.FullName,
                    ChangedByRole = resolver.Role,
                    Reason = $"Phúc khảo được duyệt. Lý do: {request.ResolutionNotes}",
                    ChangedAt = DateTime.UtcNow
                });

                // If reviewer has specific rubric criteria details, update criteria and log audit
                if (appeal.ReviewerDetails.Any())
                {
                    var existingDetailsMap = submission.Grade.GradeDetails.ToDictionary(d => d.RubricCriteriaId);
                    foreach (var revDetail in appeal.ReviewerDetails)
                    {
                        if (existingDetailsMap.TryGetValue(revDetail.RubricCriteriaId, out var existingDetail))
                        {
                            if (existingDetail.Score != revDetail.Score)
                            {
                                _context.GradeAuditLogs.Add(new GradeAuditLog
                                {
                                    SubmissionId = submission.Id,
                                    GradeId = submission.Grade.Id,
                                    RubricCriteriaId = revDetail.RubricCriteriaId,
                                    CriteriaName = revDetail.RubricCriteria?.CriteriaName ?? $"Criteria #{revDetail.RubricCriteriaId}",
                                    OldScore = existingDetail.Score,
                                    NewScore = revDetail.Score,
                                    OldFeedback = existingDetail.Feedback,
                                    NewFeedback = revDetail.Feedback,
                                    ChangedById = request.ResolvedById,
                                    ChangedByName = resolver.FullName,
                                    ChangedByRole = resolver.Role,
                                    Reason = $"Cập nhật tiêu chí theo kết quả chấm phúc khảo của {appeal.ReviewerLecturer?.FullName ?? "Reviewer"}",
                                    ChangedAt = DateTime.UtcNow
                                });

                                existingDetail.Score = revDetail.Score;
                                existingDetail.Feedback = revDetail.Feedback;
                            }
                        }
                    }
                }

                // Update official grade
                submission.Grade.TotalScore = resolvedScore;
                submission.Grade.GeneralFeedback = string.IsNullOrWhiteSpace(submission.Grade.GeneralFeedback)
                    ? $"[Phúc khảo]: {request.ResolutionNotes}"
                    : $"{submission.Grade.GeneralFeedback}\n[Phúc khảo]: {request.ResolutionNotes}";
                submission.Grade.GradedAt = DateTime.UtcNow;

                appeal.Status = "Approved";
                appeal.FinalScore = resolvedScore;
            }
            else
            {
                // Rejected
                _context.GradeAuditLogs.Add(new GradeAuditLog
                {
                    SubmissionId = submission.Id,
                    GradeId = submission.Grade.Id,
                    CriteriaName = "Total Score (Grade Appeal)",
                    OldScore = appeal.OriginalScore,
                    NewScore = appeal.OriginalScore,
                    OldFeedback = submission.Grade.GeneralFeedback,
                    NewFeedback = submission.Grade.GeneralFeedback,
                    ChangedById = request.ResolvedById,
                    ChangedByName = resolver.FullName,
                    ChangedByRole = resolver.Role,
                    Reason = $"Đơn phúc khảo bị từ chối giữ nguyên điểm. Lý do: {request.ResolutionNotes}",
                    ChangedAt = DateTime.UtcNow
                });

                appeal.Status = "Rejected";
                appeal.FinalScore = appeal.OriginalScore;
            }

            appeal.ResolvedAt = DateTime.UtcNow;
            appeal.ResolvedById = request.ResolvedById;
            appeal.ResolvedBy = resolver;
            appeal.ResolutionNotes = request.ResolutionNotes;

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
                resolver.FullName,
                appeal.ResolutionNotes,
                appeal.ReviewerDetails.Select(d => new GradeAppealDetailDto(
                    d.Id,
                    d.RubricCriteriaId,
                    d.RubricCriteria?.CriteriaName ?? string.Empty,
                    d.RubricCriteria?.MaxPoints ?? 0,
                    d.Score,
                    d.Feedback
                )).ToList()
            );
        }
    }
}
