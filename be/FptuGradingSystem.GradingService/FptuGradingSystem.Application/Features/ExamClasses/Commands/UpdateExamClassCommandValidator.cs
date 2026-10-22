using FluentValidation;

namespace FptuGradingSystem.Application.Features.ExamClasses.Commands
{
    public class UpdateExamClassCommandValidator : AbstractValidator<UpdateExamClassCommand>
    {
        public UpdateExamClassCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Valid Exam Class ID is required.");

            RuleFor(x => x.ClassId)
                .GreaterThan(0).WithMessage("Valid Class ID is required.");

            RuleFor(x => x.SubjectId)
                .GreaterThan(0).WithMessage("Valid Subject ID is required.");

            RuleFor(x => x.Semester)
                .NotEmpty().WithMessage("Semester is required.")
                .MaximumLength(20).WithMessage("Semester cannot exceed 20 characters.");
        }
    }
}
