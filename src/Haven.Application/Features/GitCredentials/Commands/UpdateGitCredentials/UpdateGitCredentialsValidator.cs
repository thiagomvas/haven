using FluentValidation;

namespace Haven.Application.Features.GitCredentials.Commands.UpdateGitCredentials;

public sealed class UpdateGitCredentialsValidator : AbstractValidator<UpdateGitCredentialsCommand>
{
    public UpdateGitCredentialsValidator()
    {
        RuleFor(x => x.DisplayName.Value)
            .NotEmpty()
            .WithMessage("Display name cannot be empty.")
            .MinimumLength(3)
            .WithMessage("Display name must be at least 3 characters.")
            .MaximumLength(256)
            .WithMessage("Display name cannot exceed 256 characters.")
            .When(x => x.DisplayName.HasValue);
    }
}