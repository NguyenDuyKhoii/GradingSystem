using FptuGradingSystem.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.Application.Features.Rubrics.Queries
{
    public record GetRubricBySubjectQuery(int SubjectId) : IRequest<RubricDto?>;

    public record RubricDto(
        int Id,
        int SubjectId,
        string Name,
        decimal TotalWeight,
        List<RubricCriteriaDto> Criteria);

    public record RubricCriteriaDto(
        int Id,
        string CriteriaName,
        string Description,
        decimal MaxPoints,
        decimal Weight);

    public class GetRubricBySubjectQueryHandler : IRequestHandler<GetRubricBySubjectQuery, RubricDto?>
    {
        private readonly IApplicationDbContext _context;

        public GetRubricBySubjectQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<RubricDto?> Handle(GetRubricBySubjectQuery request, CancellationToken cancellationToken)
        {
            var rubric = await _context.Rubrics
                .Include(r => r.Criteria)
                .FirstOrDefaultAsync(r => r.SubjectId == request.SubjectId, cancellationToken);

            if (rubric == null) return null;

            return new RubricDto(
                rubric.Id,
                rubric.SubjectId,
                rubric.Name,
                rubric.TotalWeight,
                rubric.Criteria.Select(c => new RubricCriteriaDto(
                    c.Id,
                    c.CriteriaName,
                    c.Description,
                    c.MaxPoints,
                    c.Weight)).ToList()
            );
        }
    }
}
