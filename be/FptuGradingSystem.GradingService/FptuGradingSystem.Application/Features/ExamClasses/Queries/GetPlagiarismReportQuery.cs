using FptuGradingSystem.Application.Common.Interfaces;
using FptuGradingSystem.Application.Features.ExamClasses.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.Application.Features.ExamClasses.Queries
{
    public record GetPlagiarismReportQuery(int ExamClassId) : IRequest<PlagiarismReportDto?>;

    public class GetPlagiarismReportQueryHandler : IRequestHandler<GetPlagiarismReportQuery, PlagiarismReportDto?>
    {
        private readonly IApplicationDbContext _context;

        public GetPlagiarismReportQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PlagiarismReportDto?> Handle(GetPlagiarismReportQuery request, CancellationToken cancellationToken)
        {
            var report = await _context.PlagiarismReports
                .AsNoTracking()
                .Include(r => r.Matches)
                .Where(r => r.ExamClassId == request.ExamClassId)
                .OrderByDescending(r => r.GeneratedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (report == null) return null;

            return new PlagiarismReportDto(
                report.Id,
                report.ExamClassId,
                report.GeneratedAt,
                report.ThresholdPercentage,
                report.TotalSubmissionsAnalyzed,
                report.SuspectPairsCount,
                report.Matches
                    .OrderByDescending(m => m.SimilarityPercentage)
                    .Select(m => new PlagiarismMatchDto(
                        m.Id,
                        m.SubmissionAId,
                        m.StudentAId,
                        m.StudentAName,
                        m.SubmissionBId,
                        m.StudentBId,
                        m.StudentBName,
                        m.SimilarityPercentage,
                        m.Details
                    )).ToList()
            );
        }
    }
}
