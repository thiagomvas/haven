using FluentValidation;
using Haven.Domain.Aggregates;

namespace Haven.Application.Features.Auth.Commands.InitialSetupCommand;

public class InitialSetupValidator : AbstractValidator<InitialSetupCommand>
{
    public InitialSetupValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(User.MaxNameLength).WithMessage($"Name cannot exceed {User.MaxNameLength} characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.");
    }
}
