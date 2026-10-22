using FluentValidation;

namespace FptuGradingSystem.Application.Features.Classes.Commands
{
    public class UpdateClassCommandValidator : AbstractValidator<UpdateClassCommand>
    {
        public UpdateClassCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Valid Class ID is required.");

            RuleFor(x => x.ClassCode)
                .NotEmpty().WithMessage("Class code is required.")
                .MaximumLength(50).WithMessage("Class code cannot exceed 50 characters.");
        }
    }
}
