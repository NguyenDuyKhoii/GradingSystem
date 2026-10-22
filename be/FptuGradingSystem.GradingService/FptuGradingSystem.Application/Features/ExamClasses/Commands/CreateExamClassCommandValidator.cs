using FluentValidation;

namespace FptuGradingSystem.Application.Features.ExamClasses.Commands
{
    public class CreateExamClassCommandValidator : AbstractValidator<CreateExamClassCommand>
    {
        public CreateExamClassCommandValidator()
        {
            RuleFor(x => x.Semester)
                .NotEmpty().WithMessage("Semester is required.")
                .MaximumLength(20).WithMessage("Semester cannot exceed 20 characters.");

            RuleFor(x => x.SubjectId)
                .GreaterThan(0).WithMessage("Valid SubjectId is required.");

            RuleFor(x => x)
                .Must(x => (x.ClassId.HasValue && x.ClassId.Value > 0) || !string.IsNullOrWhiteSpace(x.ClassCode))
                .WithMessage("Either a valid Class ID or Class Code must be provided.");

            When(x => !string.IsNullOrWhiteSpace(x.ClassCode), () =>
            {
                RuleFor(x => x.ClassCode)
                    .MaximumLength(50).WithMessage("Class code cannot exceed 50 characters.");
            });
        }
    }
}
