using FptuGradingSystem.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.Application.Features.ExamClasses.Commands
{
    public record FinalizeExamClassGradesCommand(int ExamClassId) : IRequest<bool>;

    public class FinalizeExamClassGradesCommandHandler : IRequestHandler<FinalizeExamClassGradesCommand, bool>
    {
        private readonly IApplicationDbContext _context;

        public FinalizeExamClassGradesCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(FinalizeExamClassGradesCommand request, CancellationToken cancellationToken)
        {
            var examClass = await _context.ExamClasses
                .Include(ec => ec.Appeals)
                .FirstOrDefaultAsync(ec => ec.Id == request.ExamClassId, cancellationToken);

            if (examClass == null) return false;

            // Check if there are still pending or assigned appeals
            var pendingAppealsCount = examClass.Appeals
                .Count(a => a.Status != "Approved" && a.Status != "Rejected");

            if (pendingAppealsCount > 0)
            {
                throw new InvalidOperationException($"Không thể chốt điểm: Còn {pendingAppealsCount} đơn phúc khảo chưa được hoàn tất xử lý.");
            }

            examClass.Status = "Finalized";
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
