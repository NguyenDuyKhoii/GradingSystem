using FptuGradingSystem.Application.Common.Interfaces;
using FptuGradingSystem.Application.Features.Appeals.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.Application.Features.Appeals.Queries
{
    public record GetExamClassAppealsQuery(int ExamClassId) : IRequest<List<GradeAppealDto>>;

    public class GetExamClassAppealsQueryHandler : IRequestHandler<GetExamClassAppealsQuery, List<GradeAppealDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetExamClassAppealsQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<GradeAppealDto>> Handle(GetExamClassAppealsQuery request, CancellationToken cancellationToken)
        {
            var appeals = await _context.GradeAppeals
                .AsNoTracking()
                .Include(a => a.ReviewerLecturer)
                .Include(a => a.ResolvedBy)
                .Include(a => a.ReviewerDetails)
                    .ThenInclude(d => d.RubricCriteria)
                .Where(a => a.ExamClassId == request.ExamClassId)
                .OrderByDescending(a => a.RequestedAt)
                .ToListAsync(cancellationToken);

            return appeals.Select(a => new GradeAppealDto(
                a.Id,
                a.ExamClassId,
                a.SubmissionId,
                a.StudentId,
                a.StudentName,
                a.StudentEmail,
                a.Reason,
                a.RequestedAt,
                a.Status,
                a.OriginalScore,
                a.OriginalGradedById,
                a.ReviewerLecturerId,
                a.ReviewerLecturer?.FullName,
                a.ReviewerScore,
                a.ReviewerFeedback,
                a.ReviewedAt,
                a.FinalScore,
                a.ResolvedAt,
                a.ResolvedById,
                a.ResolvedBy?.FullName,
                a.ResolutionNotes,
                a.ReviewerDetails.Select(d => new GradeAppealDetailDto(
                    d.Id,
                    d.RubricCriteriaId,
                    d.RubricCriteria?.CriteriaName ?? string.Empty,
                    d.RubricCriteria?.MaxPoints ?? 0,
                    d.Score,
                    d.Feedback
                )).ToList()
            )).ToList();
        }
    }
}
