using FptuGradingSystem.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.Application.Features.Appeals.Commands
{
    public record AssignAppealReviewerCommand(
        int AppealId,
        int ReviewerLecturerId) : IRequest<GradeAppealDto>;

    public class AssignAppealReviewerCommandHandler : IRequestHandler<AssignAppealReviewerCommand, GradeAppealDto>
    {
        private readonly IApplicationDbContext _context;

        public AssignAppealReviewerCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<GradeAppealDto> Handle(AssignAppealReviewerCommand request, CancellationToken cancellationToken)
        {
            var appeal = await _context.GradeAppeals
                .Include(a => a.ReviewerDetails)
                .Include(a => a.ReviewerLecturer)
                .Include(a => a.ResolvedBy)
                .FirstOrDefaultAsync(a => a.Id == request.AppealId, cancellationToken);

            if (appeal == null)
            {
                throw new KeyNotFoundException($"Đơn phúc khảo #{request.AppealId} không tồn tại.");
            }

            if (appeal.Status == "Approved" || appeal.Status == "Rejected")
            {
                throw new InvalidOperationException($"Đơn phúc khảo đã kết thúc ({appeal.Status}), không thể phân công lại.");
            }

            var reviewer = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.ReviewerLecturerId, cancellationToken);
            if (reviewer == null || reviewer.Role != "Lecturer")
            {
                throw new ArgumentException("Giảng viên được phân công không hợp lệ hoặc không có quyền Lecturer.");
            }

            // Dual-Grading Integrity rule: Grader 2 cannot be the same as Grader 1
            if (appeal.OriginalGradedById.HasValue && appeal.OriginalGradedById.Value == request.ReviewerLecturerId)
            {
                throw new InvalidOperationException(
                    "Nguyên tắc Chấm chéo độc lập (Blind Dual-Grading): Giảng viên chấm phúc khảo (Reviewer) không được trùng với Giảng viên đã chấm ban đầu.");
            }

            appeal.ReviewerLecturerId = request.ReviewerLecturerId;
            appeal.ReviewerLecturer = reviewer;
            appeal.Status = "Assigned";

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
                reviewer.FullName,
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
                    d.RubricCriteria?.CriteriaName ?? string.Empty,
                    d.RubricCriteria?.MaxPoints ?? 0,
                    d.Score,
                    d.Feedback
                )).ToList()
            );
        }
    }
}
