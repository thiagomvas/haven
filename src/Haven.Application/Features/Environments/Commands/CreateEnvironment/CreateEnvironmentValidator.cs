using FluentValidation;

using Haven.Domain.Entities;

using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Application.Features.Environments.Commands.CreateEnvironment;

public sealed class CreateEnvironmentValidator : AbstractValidator<CreateEnvironmentCommand>
{
    private static readonly HashSet<string> ReservedNames =
        new(StringComparer.OrdinalIgnoreCase) { "haven", "shared", "internal", "host" };

    public CreateEnvironmentValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty()
            .WithMessage("Project ID cannot be empty.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Environment name cannot be empty.")
            .MaximumLength(Environment.MaxNameLength)
            .WithMessage($"Environment name cannot exceed {Environment.MaxNameLength} characters.")
            .Must(name => !ReservedNames.Contains(name))
            .WithMessage("Environment name is reserved and cannot be used.");

        RuleFor(x => x.Alias)
            .MinimumLength(2)
            .WithMessage("Environment alias must be at least 2 characters.")
            .MaximumLength(8)
            .WithMessage("Environment alias cannot exceed 8 characters.")
            .Matches(@"^[a-z0-9][a-z0-9-]*[a-z0-9]$")
            .WithMessage("Environment alias may only contain lowercase letters, digits, and hyphens, and cannot start or end with a hyphen.")
            .When(x => !string.IsNullOrEmpty(x.Alias));

        RuleFor(x => x.Description)
            .MaximumLength(Environment.MaxDescriptionLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Description))
            .WithMessage($"Environment description cannot exceed {Environment.MaxDescriptionLength} characters.");
    }
}