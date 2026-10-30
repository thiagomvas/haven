using FluentValidation;

using DomainEnvironment = Haven.Domain.Aggregates.Environment;

namespace Haven.Application.Features.Environments.Commands.ImportEnvironment;

public sealed class ImportEnvironmentValidator : AbstractValidator<ImportEnvironmentCommand>
{
    public ImportEnvironmentValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty()
            .WithMessage("Project ID cannot be empty.");

        RuleFor(x => x.Yaml)
            .NotEmpty()
            .WithMessage("Manifest content cannot be empty.");

        RuleFor(x => x.Name)
            .MaximumLength(DomainEnvironment.MaxNameLength)
            .WithMessage($"Environment name cannot exceed {DomainEnvironment.MaxNameLength} characters.")
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.Alias)
            .MaximumLength(8)
            .WithMessage("Environment alias cannot exceed 8 characters.")
            .Matches(@"^[a-z0-9][a-z0-9-]*[a-z0-9]$")
            .WithMessage("Environment alias may only contain lowercase letters, digits, and hyphens, and cannot start or end with a hyphen.")
            .When(x => !string.IsNullOrEmpty(x.Alias));
    }
}
