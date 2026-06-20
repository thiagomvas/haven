using FluentValidation;

using Haven.Domain.Entities;

namespace Haven.Application.Features.GitCredentials.Commands.CreateGitCredentials;

public sealed class CreateGitCredentialsValidator : AbstractValidator<CreateGitCredentialsCommand>
{
    public CreateGitCredentialsValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .WithMessage("Display name cannot be empty.")
            .MinimumLength(3)
            .WithMessage("Display name must be at least 3 characters.")
            .MaximumLength(256)
            .WithMessage("Display name cannot exceed 256 characters.");

        RuleFor(x => x.PrimaryCredential)
            .NotEmpty()
            .WithMessage("Primary credential cannot be empty.");

        RuleFor(x => x.HostUrl)
            .Must(url => url == null || Uri.TryCreate(url, UriKind.Absolute, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.HostUrl))
            .WithMessage("Host URL must be a valid absolute URI.");

        RuleFor(x => x.AuthMethod)
            .IsInEnum()
            .WithMessage("Auth method must be a valid value.");

        RuleFor(x => x.ProviderType)
            .IsInEnum()
            .WithMessage("Provider type must be a valid value.");

        RuleFor(x => x.SecondaryCredential)
            .NotEmpty()
            .When(x => x.SecondaryCredential != null)
            .WithMessage("Secondary credential cannot be empty if provided.");
    }
}