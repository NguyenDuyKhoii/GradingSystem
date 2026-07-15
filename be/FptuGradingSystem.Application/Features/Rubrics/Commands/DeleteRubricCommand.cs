using FptuGradingSystem.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.Application.Features.Rubrics.Commands
{
    public record DeleteRubricCommand(int Id) : IRequest<Unit>;

    public class DeleteRubricCommandHandler : IRequestHandler<DeleteRubricCommand, Unit>
    {
        private readonly IApplicationDbContext _context;

        public DeleteRubricCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Unit> Handle(DeleteRubricCommand request, CancellationToken cancellationToken)
        {
            var rubric = await _context.Rubrics
                .Include(r => r.Criteria)
                .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
                ?? throw new KeyNotFoundException($"Rubric with ID {request.Id} not found.");

            // Check if any criteria have grade details
            var criteriaIds = rubric.Criteria.Select(c => c.Id).ToList();
            var hasGrades = await _context.GradeDetails
                .AnyAsync(gd => criteriaIds.Contains(gd.RubricCriteriaId), cancellationToken);

            if (hasGrades)
            {
                throw new ArgumentException("Cannot delete rubric because grades have already been recorded for its criteria.");
            }

            _context.RubricCriteria.RemoveRange(rubric.Criteria);
            _context.Rubrics.Remove(rubric);
            await _context.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
