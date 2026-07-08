using FluentValidation;

using Haven.Domain;

namespace Haven.Application.Features.GitCredentials.Commands.RotateGitCredentials;

public sealed class RotateGitCredentialsValidator : AbstractValidator<RotateGitCredentialsCommand>
{
    public RotateGitCredentialsValidator()
    {
        RuleFor(x => x.PrimaryCredential)
            .NotEmpty()
            .WithMessage("Primary credential cannot be empty.");

        RuleFor(x => x.AuthMethod)
            .IsInEnum()
            .WithMessage("Auth method must be a valid value.")
            .NotEqual(GitAuthMethod.OAuth)
            .WithMessage("OAuth credentials are rotated by reconnecting the provider, not by submitting a secret manually.");

        RuleFor(x => x.SecondaryCredential)
            .NotEmpty()
            .When(x => x.SecondaryCredential != null)
            .WithMessage("Secondary credential cannot be empty if provided.");
    }
}