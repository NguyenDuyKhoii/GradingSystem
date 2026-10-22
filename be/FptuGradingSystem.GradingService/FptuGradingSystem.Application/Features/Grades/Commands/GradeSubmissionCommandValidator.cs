using FluentValidation;

namespace FptuGradingSystem.Application.Features.Grades.Commands
{
    public class GradeSubmissionCommandValidator : AbstractValidator<GradeSubmissionCommand>
    {
        public GradeSubmissionCommandValidator()
        {
            RuleFor(x => x.SubmissionId)
                .GreaterThan(0).WithMessage("Valid Submission ID is required.");

            RuleFor(x => x.GradedById)
                .GreaterThan(0).WithMessage("Valid GradedBy ID is required.");

            RuleFor(x => x.Details)
                .NotNull().WithMessage("Grade details cannot be null.");

            RuleForEach(x => x.Details).ChildRules(detail =>
            {
                detail.RuleFor(d => d.RubricCriteriaId)
                    .GreaterThan(0).WithMessage("Valid Rubric Criteria ID is required.");

                detail.RuleFor(d => d.Score)
                    .GreaterThanOrEqualTo(0).WithMessage("Score cannot be negative.");
            });
        }
    }
}
