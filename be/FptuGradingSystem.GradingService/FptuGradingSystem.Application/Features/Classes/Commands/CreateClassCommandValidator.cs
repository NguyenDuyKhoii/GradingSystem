using FluentValidation;

namespace FptuGradingSystem.Application.Features.Classes.Commands
{
    public class CreateClassCommandValidator : AbstractValidator<CreateClassCommand>
    {
        public CreateClassCommandValidator()
        {
            RuleFor(x => x.ClassCode)
                .NotEmpty().WithMessage("Class code is required.")
                .MaximumLength(50).WithMessage("Class code cannot exceed 50 characters.");
        }
    }
}
