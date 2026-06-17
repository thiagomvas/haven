using FluentValidation;

using Haven.Domain.Aggregates;

namespace Haven.Application.Features.Projects.Commands.CloneProject;

public sealed class CloneProjectValidator : AbstractValidator<CloneProjectCommand>
{
    public CloneProjectValidator()
    {
        RuleFor(x => x.NewName)
            .NotEmpty()
            .WithMessage("Project name cannot be empty.")
            .MinimumLength(Project.MinNameLength)
            .WithMessage($"Project name must be at least {Project.MinNameLength} characters.")
            .MaximumLength(Project.MaxNameLength)
            .WithMessage($"Project name cannot exceed {Project.MaxNameLength} characters.");

        RuleFor(x => x.NewAlias)
            .NotEmpty()
            .WithMessage("Project alias is required.")
            .MinimumLength(Project.MinAliasLength)
            .WithMessage($"Project alias must be at least {Project.MinAliasLength} characters.")
            .MaximumLength(Project.MaxAliasLength)
            .WithMessage($"Project alias cannot exceed {Project.MaxAliasLength} characters.")
            .Matches(@"^[a-z0-9][a-z0-9-]*[a-z0-9]$")
            .WithMessage("Project alias may only contain lowercase letters, digits, and hyphens, and cannot start or end with a hyphen.");
    }
}
