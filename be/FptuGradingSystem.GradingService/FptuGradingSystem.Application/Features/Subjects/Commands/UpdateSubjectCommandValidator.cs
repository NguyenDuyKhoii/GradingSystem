using FluentValidation;

namespace FptuGradingSystem.Application.Features.Subjects.Commands
{
    public class UpdateSubjectCommandValidator : AbstractValidator<UpdateSubjectCommand>
    {
        public UpdateSubjectCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Valid Subject ID is required.");

            RuleFor(x => x.SubjectCode)
                .NotEmpty().WithMessage("Subject code is required.")
                .MaximumLength(20).WithMessage("Subject code cannot exceed 20 characters.");

            RuleFor(x => x.SubjectName)
                .NotEmpty().WithMessage("Subject name is required.")
                .MaximumLength(150).WithMessage("Subject name cannot exceed 150 characters.");
        }
    }
}
