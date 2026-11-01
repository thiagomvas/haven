using FluentValidation;

using Haven.Domain.Aggregates;

namespace Haven.Application.Features.Projects.Commands.ImportProject;

public sealed class ImportProjectValidator : AbstractValidator<ImportProjectCommand>
{
    public ImportProjectValidator()
    {
        RuleFor(x => x.Yaml)
            .NotEmpty()
            .WithMessage("Manifest content cannot be empty.");

        RuleFor(x => x.Name)
            .MinimumLength(Project.MinNameLength)
            .WithMessage($"Project name must be at least {Project.MinNameLength} characters.")
            .MaximumLength(Project.MaxNameLength)
            .WithMessage($"Project name cannot exceed {Project.MaxNameLength} characters.")
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.Alias)
            .MinimumLength(Project.MinAliasLength)
            .WithMessage($"Project alias must be at least {Project.MinAliasLength} characters.")
            .MaximumLength(Project.MaxAliasLength)
            .WithMessage($"Project alias cannot exceed {Project.MaxAliasLength} characters.")
            .Matches(@"^[a-z0-9][a-z0-9-]*[a-z0-9]$")
            .WithMessage("Project alias may only contain lowercase letters, digits, and hyphens, and cannot start or end with a hyphen.")
            .When(x => !string.IsNullOrEmpty(x.Alias));

        RuleFor(x => x.IncludeEnvironments)
            .Equal(true)
            .When(x => x.IncludeServices)
            .WithMessage("Services can only be imported when environments are imported.");
    }
}
