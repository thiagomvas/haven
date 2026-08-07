using FluentValidation;

using Environment = Haven.Domain.Aggregates.Environment;

namespace Haven.Application.Features.Environments.Commands.UpdateEnvironment;

public sealed class UpdateEnvironmentValidator : AbstractValidator<UpdateEnvironmentCommand>
{
    private static readonly HashSet<string> ReservedNames =
        new(StringComparer.OrdinalIgnoreCase) { "haven", "shared", "internal", "host" };

    public UpdateEnvironmentValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.EnvironmentId).NotEmpty();

        RuleFor(x => x.Name)
            .Must(n => !string.IsNullOrWhiteSpace(n.Value))
            .When(x => x.Name.HasValue)
            .WithMessage("Environment name cannot be empty.")

            .Must(n => n.Value == null || n.Value.Length <= Environment.MaxNameLength)
            .When(x => x.Name.HasValue)
            .WithMessage($"Environment name cannot exceed {Environment.MaxNameLength} characters.")

            .Must(n => n.Value == null || !ReservedNames.Contains(n.Value))
            .When(x => x.Name.HasValue)
            .WithMessage("Environment name is reserved and cannot be used.");

        RuleFor(x => x.Description)
            .Must(d => d.Value == null || d.Value.Length <= Environment.MaxDescriptionLength)
            .When(x => x.Description.HasValue)
            .WithMessage($"Environment description cannot exceed {Environment.MaxDescriptionLength} characters.");
    }
}