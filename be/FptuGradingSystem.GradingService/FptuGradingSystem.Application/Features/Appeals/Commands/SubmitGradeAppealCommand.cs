using FptuGradingSystem.Application.Common.Interfaces;
using FptuGradingSystem.Domain.Entities;
using FptuGradingSystem.Domain.Helpers;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.Application.Features.Appeals.Commands
{
    public record SubmitGradeAppealCommand(
        int ExamClassId,
        int SubmissionId,
        string StudentId,
        string StudentName,
        string Reason,
        string? StudentEmail = null) : IRequest<GradeAppealDto>;

    public record GradeAppealDto(
        int Id,
        int ExamClassId,
        int SubmissionId,
        string StudentId,
        string StudentName,
        string StudentEmail,
        string Reason,
        DateTime RequestedAt,
        string Status,
        decimal OriginalScore,
        int? OriginalGradedById,
        int? ReviewerLecturerId,
        string? ReviewerLecturerName,
        decimal? ReviewerScore,
        string? ReviewerFeedback,
        DateTime? ReviewedAt,
        decimal? FinalScore,
        DateTime? ResolvedAt,
        int? ResolvedById,
        string? ResolvedByName,
        string? ResolutionNotes,
        List<GradeAppealDetailDto>? ReviewerDetails = null);

    public record GradeAppealDetailDto(
        int Id,
        int RubricCriteriaId,
        string CriteriaName,
        decimal MaxPoints,
        decimal Score,
        string Feedback);

    public class SubmitGradeAppealCommandHandler : IRequestHandler<SubmitGradeAppealCommand, GradeAppealDto>
    {
        private readonly IApplicationDbContext _context;

        public SubmitGradeAppealCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<GradeAppealDto> Handle(SubmitGradeAppealCommand request, CancellationToken cancellationToken)
        {
            var examClass = await _context.ExamClasses
                .Include(ec => ec.Submissions)
                    .ThenInclude(s => s.Grade)
                .FirstOrDefaultAsync(ec => ec.Id == request.ExamClassId, cancellationToken);

            if (examClass == null)
            {
                throw new KeyNotFoundException($"Exam class #{request.ExamClassId} not found.");
            }

            if (examClass.Status != "GradesPublished" && examClass.Status != "AppealOpen")
            {
                throw new InvalidOperationException($"Lớp thi đang ở trạng thái '{examClass.Status}'. Chỉ có thể gửi đơn phúc khảo khi lớp thi ở trạng thái GradesPublished hoặc AppealOpen.");
            }

            if (examClass.AppealDeadline.HasValue && DateTime.UtcNow > examClass.AppealDeadline.Value)
            {
                throw new InvalidOperationException($"Thời hạn tiếp nhận đơn phúc khảo của lớp thi đã kết thúc vào lúc {examClass.AppealDeadline.Value:yyyy-MM-dd HH:mm:ss} UTC (quy định 48h sau công bố điểm).");
            }

            var submission = examClass.Submissions.FirstOrDefault(s => s.Id == request.SubmissionId);
            if (submission == null)
            {
                throw new KeyNotFoundException($"Bài nộp #{request.SubmissionId} không thuộc lớp thi này.");
            }

            if (submission.Grade == null)
            {
                throw new InvalidOperationException("Bài nộp này chưa có điểm để phúc khảo.");
            }

            if (!submission.StudentId.Equals(request.StudentId.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Mã số sinh viên '{request.StudentId}' không khớp với bài thi ({submission.StudentId}).");
            }

            var hasActiveAppeal = await _context.GradeAppeals
                .AnyAsync(a => a.SubmissionId == request.SubmissionId &&
                               a.Status != "Approved" &&
                               a.Status != "Rejected", cancellationToken);

            if (hasActiveAppeal)
            {
                throw new InvalidOperationException("Bài thi này đang có đơn phúc khảo đang được xem xét xử lý, không thể tạo thêm đơn mới.");
            }

            string studentEmail = !string.IsNullOrWhiteSpace(request.StudentEmail)
                ? request.StudentEmail.Trim()
                : (!string.IsNullOrWhiteSpace(submission.Email)
                    ? submission.Email
                    : FptuEmailHelper.GenerateFptuEmail(request.StudentName, request.StudentId));

            var appeal = new GradeAppeal
            {
                ExamClassId = request.ExamClassId,
                SubmissionId = request.SubmissionId,
                StudentId = request.StudentId.Trim(),
                StudentName = request.StudentName.Trim(),
                StudentEmail = studentEmail,
                Reason = request.Reason,
                RequestedAt = DateTime.UtcNow,
                Status = "Pending",
                OriginalScore = submission.Grade.TotalScore,
                OriginalGradedById = submission.Grade.GradedById
            };

            _context.GradeAppeals.Add(appeal);
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
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                new List<GradeAppealDetailDto>()
            );
        }
    }
}
