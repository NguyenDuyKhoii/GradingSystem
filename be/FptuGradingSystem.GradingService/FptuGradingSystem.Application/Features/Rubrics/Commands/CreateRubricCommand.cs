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
    public record CreateRubricCommand(
        int SubjectId,
        string Name,
        List<CreateRubricCriteriaDto> Criteria) : IRequest<int>;

    public record CreateRubricCriteriaDto(
        string CriteriaName,
        string Description,
        decimal MaxPoints,
        decimal Weight);

    public class CreateRubricCommandHandler : IRequestHandler<CreateRubricCommand, int>
    {
        private readonly IApplicationDbContext _context;

        public CreateRubricCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> Handle(CreateRubricCommand request, CancellationToken cancellationToken)
        {
            var subjectExists = await _context.Subjects
                .AnyAsync(s => s.Id == request.SubjectId, cancellationToken);

            if (!subjectExists)
            {
                throw new KeyNotFoundException($"Subject with ID {request.SubjectId} not found.");
            }

            // Validate total weight is either 1.0 (decimal representation) or 100 (percentage)
            decimal totalWeight = request.Criteria.Sum(c => c.Weight);
            if (Math.Abs(totalWeight - 1.0m) > 0.001m && Math.Abs(totalWeight - 100.0m) > 0.001m)
            {
                throw new ArgumentException("Total weight of all criteria must sum up to 1.0 (decimal) or 100 (percentage).");
            }

            var rubric = new Rubric
            {
                SubjectId = request.SubjectId,
                Name = request.Name,
                TotalWeight = totalWeight
            };

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

            _context.Rubrics.Add(rubric);
            await _context.SaveChangesAsync(cancellationToken);

            return rubric.Id;
        }
    }
}
