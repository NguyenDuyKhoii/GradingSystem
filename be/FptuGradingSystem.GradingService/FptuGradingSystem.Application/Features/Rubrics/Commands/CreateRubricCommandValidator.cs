using FluentValidation;
using System;
using System.Linq;

namespace FptuGradingSystem.Application.Features.Rubrics.Commands
{
    public class CreateRubricCommandValidator : AbstractValidator<CreateRubricCommand>
    {
        public CreateRubricCommandValidator()
        {
            RuleFor(x => x.SubjectId)
                .GreaterThan(0).WithMessage("Valid Subject ID is required.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Rubric name is required.")
                .MaximumLength(100).WithMessage("Rubric name cannot exceed 100 characters.");

            RuleFor(x => x.Criteria)
                .NotEmpty().WithMessage("At least one criteria is required.");

            RuleForEach(x => x.Criteria).ChildRules(criterion =>
            {
                criterion.RuleFor(c => c.CriteriaName)
                    .NotEmpty().WithMessage("Criteria name is required.")
                    .MaximumLength(100).WithMessage("Criteria name cannot exceed 100 characters.");

                criterion.RuleFor(c => c.MaxPoints)
                    .GreaterThan(0).WithMessage("Max points must be greater than 0.");

                criterion.RuleFor(c => c.Weight)
                    .GreaterThan(0).WithMessage("Weight must be greater than 0.");
            });

            RuleFor(x => x.Criteria)
                .Must(criteria =>
                {
                    if (criteria == null || criteria.Count == 0) return true;
                    var totalWeight = criteria.Sum(c => c.Weight);
                    return Math.Abs(totalWeight - 1.0m) <= 0.001m || Math.Abs(totalWeight - 100.0m) <= 0.001m;
                })
                .WithMessage("Total weight of all criteria must sum up to 1.0 (decimal) or 100 (percentage).");
        }
    }
}
