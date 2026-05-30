using FluentValidation;

namespace Haven.Application.Features.Auth.Commands.SetPasswordCommand;

public sealed class SetPasswordValidator : AbstractValidator<SetPasswordCommand>
{
    public SetPasswordValidator()
    {
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword).WithMessage("Passwords do not match.");
    }
}
