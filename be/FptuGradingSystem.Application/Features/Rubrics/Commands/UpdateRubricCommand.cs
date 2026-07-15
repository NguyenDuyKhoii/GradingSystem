using FptuGradingSystem.Application.Common.Interfaces;
using FptuGradingSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.Application.Features.Rubrics.Commands
{
    public record UpdateRubricCommand(
        int Id,
        string Name,
        List<CreateRubricCriteriaDto> Criteria) : IRequest<Unit>;

    public class UpdateRubricCommandHandler : IRequestHandler<UpdateRubricCommand, Unit>
    {
        private readonly IApplicationDbContext _context;

        public UpdateRubricCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Unit> Handle(UpdateRubricCommand request, CancellationToken cancellationToken)
        {
            var rubric = await _context.Rubrics
                .Include(r => r.Criteria)
                .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
                ?? throw new KeyNotFoundException($"Rubric with ID {request.Id} not found.");

            // Check if any existing criteria have grade details (block update if grading has started)
            var criteriaIds = rubric.Criteria.Select(c => c.Id).ToList();
            var hasGrades = await _context.GradeDetails
                .AnyAsync(gd => criteriaIds.Contains(gd.RubricCriteriaId), cancellationToken);

            if (hasGrades)
            {
                throw new ArgumentException("Cannot update rubric criteria because grades have already been recorded.");
            }

            // Validate total weight
            decimal totalWeight = request.Criteria.Sum(c => c.Weight);
            if (Math.Abs(totalWeight - 1.0m) > 0.001m && Math.Abs(totalWeight - 100.0m) > 0.001m)
            {
                throw new ArgumentException("Total weight of all criteria must sum up to 1.0 (decimal) or 100 (percentage).");
            }

            // Remove old criteria and add new ones
            _context.RubricCriteria.RemoveRange(rubric.Criteria);

            rubric.Name = request.Name;
            rubric.TotalWeight = totalWeight;

            foreach (var item in request.Criteria)
            {
                rubric.Criteria.Add(new RubricCriteria
                {
                    CriteriaName = item.CriteriaName,
                    Description = item.Description,
                    MaxPoints = item.MaxPoints,
                    Weight = item.Weight
                });
            }

            await _context.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
