using FluentValidation;

namespace Haven.Application.Features.Auth.Commands.RefreshTokenCommand;

public class RefreshTokenValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Refresh token is required.");
    }
}