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

        RuleFor(x => x.Description)
            .MaximumLength(Environment.MaxDescriptionLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Description))
            .WithMessage($"Environment description cannot exceed {Environment.MaxDescriptionLength} characters.");
    }
}
