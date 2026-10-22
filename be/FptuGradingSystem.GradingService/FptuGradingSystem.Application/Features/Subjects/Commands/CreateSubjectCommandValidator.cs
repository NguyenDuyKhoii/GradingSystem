using FluentValidation;

namespace FptuGradingSystem.Application.Features.Subjects.Commands
{
    public class CreateSubjectCommandValidator : AbstractValidator<CreateSubjectCommand>
    {
        public CreateSubjectCommandValidator()
        {
            RuleFor(x => x.SubjectCode)
                .NotEmpty().WithMessage("Subject code is required.")
                .MaximumLength(20).WithMessage("Subject code cannot exceed 20 characters.");

            RuleFor(x => x.SubjectName)
                .NotEmpty().WithMessage("Subject name is required.")
                .MaximumLength(150).WithMessage("Subject name cannot exceed 150 characters.");
        }
    }
}
