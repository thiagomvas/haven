using System.Text.RegularExpressions;
using FluentValidation;
using Haven.Domain.Aggregates;


namespace Haven.Application.Features.Projects.Commands.UpdateProject;

public sealed class UpdateProjectValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectValidator()
    {
        RuleFor(x => x.Name)
            .Must(n => !string.IsNullOrWhiteSpace(n.Value))
            .When(x => x.Name.HasValue)
            .WithMessage("Project name cannot be empty.")
            
            .Must(n => n.Value == null || n.Value.Length >= Project.MinNameLength)
            .When(x => x.Name.HasValue)
            .WithMessage($"Project name must be at least {Project.MinNameLength} characters.")

            .Must(n => n.Value == null || n.Value.Length <= Project.MaxNameLength)
            .When(x => x.Name.HasValue)
            .WithMessage($"Project name cannot exceed {Project.MaxNameLength} characters.");
        
        RuleFor(x => x.Alias)
            .Must(a => a.Value == null || a.Value.Length >= Project.MinAliasLength)
            .When(x => x.Alias.HasValue)
            .WithMessage($"Project alias must be at least {Project.MinAliasLength} characters.")
            .Must(a => a.Value == null || a.Value.Length <= Project.MaxAliasLength)
            .When(x => x.Alias.HasValue)
            .WithMessage($"Project alias cannot exceed {Project.MaxAliasLength} characters.")
            .Must(a => a.Value == null || Regex.IsMatch(a.Value, @"^[a-z0-9][a-z0-9-]*[a-z0-9]$"))
            .When(x => x.Alias.HasValue)
            .WithMessage("Project alias may only contain lowercase letters, digits, and hyphens, and cannot start or end with a hyphen.");

        RuleFor(x => x.Description)
            .Must(d => d.Value == null || d.Value.Length <= Project.MaxDescriptionLength)
            .When(x => x.Description.HasValue)
            .WithMessage($"Project description cannot exceed {Project.MaxDescriptionLength} characters.");
    }
}