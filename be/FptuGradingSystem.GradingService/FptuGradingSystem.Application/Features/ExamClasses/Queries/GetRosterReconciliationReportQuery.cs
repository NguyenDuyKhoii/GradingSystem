using FptuGradingSystem.Application.Common.Interfaces;
using FptuGradingSystem.Application.Features.ExamClasses.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.Application.Features.ExamClasses.Queries
{
    public record GetRosterReconciliationReportQuery(int ExamClassId) : IRequest<RosterReconciliationReportDto?>;

    public class GetRosterReconciliationReportQueryHandler
        : IRequestHandler<GetRosterReconciliationReportQuery, RosterReconciliationReportDto?>
    {
        private readonly IApplicationDbContext _context;

        public GetRosterReconciliationReportQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<RosterReconciliationReportDto?> Handle(
            GetRosterReconciliationReportQuery request,
            CancellationToken cancellationToken)
        {
            var report = await _context.RosterReconciliationReports
                .AsNoTracking()
                .Include(r => r.Items)
                .Where(r => r.ExamClassId == request.ExamClassId)
                .OrderByDescending(r => r.ReconciledAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (report == null) return null;

            return new RosterReconciliationReportDto(
                report.Id,
                report.ExamClassId,
                report.ReconciledAt,
                report.TotalInRoster,
                report.TotalSubmissions,
                report.MatchedCount,
                report.AbsentCount,
                report.UnrecognizedCount,
                report.Items.Select(i => new RosterReconciliationItemDto(
                    i.Id,
                    i.StudentId,
                    i.StudentName,
                    i.Email,
                    i.Status,
                    i.SubmissionId,
                    i.Notes
                )).ToList()
            );
        }
    }
}
