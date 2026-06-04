using FluentValidation;
using Haven.Domain.Aggregates;


namespace Haven.Application.Features.Projects.Commands.CreateProject;

public sealed class CreateProjectValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Project name cannot be empty.")
            .MinimumLength(Project.MinNameLength)
            .WithMessage($"Project name must be at least {Project.MinNameLength} characters.")
            .MaximumLength(Project.MaxNameLength)
            .WithMessage($"Project name cannot exceed {Project.MaxNameLength} characters.");

        RuleFor(x => x.Alias)
            .MinimumLength(Project.MinAliasLength)
            .WithMessage($"Project alias must be at least {Project.MinAliasLength} characters.")
            .MaximumLength(Project.MaxAliasLength)
            .WithMessage($"Project alias cannot exceed {Project.MaxAliasLength} characters.")
            .Matches(@"^[a-z0-9][a-z0-9-]*[a-z0-9]$")
            .WithMessage("Project alias may only contain lowercase letters, digits, and hyphens, and cannot start or end with a hyphen.")
            .When(x => !string.IsNullOrEmpty(x.Alias));

        RuleFor(x => x.Description)
            .MaximumLength(Project.MaxDescriptionLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Description))
            .WithMessage($"Project description cannot exceed {Project.MaxDescriptionLength} characters.");
    }
}