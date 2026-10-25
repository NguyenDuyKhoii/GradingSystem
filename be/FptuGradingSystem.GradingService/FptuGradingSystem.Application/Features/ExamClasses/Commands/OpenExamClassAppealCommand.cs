using FptuGradingSystem.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.Application.Features.ExamClasses.Commands
{
    public record OpenExamClassAppealCommand(int ExamClassId, int DurationHours = 48) : IRequest<bool>;

    public class OpenExamClassAppealCommandHandler : IRequestHandler<OpenExamClassAppealCommand, bool>
    {
        private readonly IApplicationDbContext _context;

        public OpenExamClassAppealCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(OpenExamClassAppealCommand request, CancellationToken cancellationToken)
        {
            var examClass = await _context.ExamClasses
                .FirstOrDefaultAsync(ec => ec.Id == request.ExamClassId, cancellationToken);

            if (examClass == null) return false;

            if (examClass.Status != "Completed" && examClass.Status != "GradesPublished" && examClass.Status != "AppealOpen")
            {
                throw new InvalidOperationException($"Không thể mở phúc khảo khi lớp thi ở trạng thái '{examClass.Status}'. Cần hoàn thành chấm điểm hoặc công bố điểm trước.");
            }

            examClass.Status = "AppealOpen";
            examClass.AppealDeadline = DateTime.UtcNow.AddHours(request.DurationHours > 0 ? request.DurationHours : 48);

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
