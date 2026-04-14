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
            .WithMessage("Project name cannot be empty.");

        RuleFor(x => x.Name)
            .Must(n => n.Value.Length <= Project.MaxNameLength)
            .When(x => x.Name.HasValue)
            .WithMessage($"Project name cannot exceed {Project.MaxNameLength} characters.");

        RuleFor(x => x.Description)
            .Must(d => d.Value is null || d.Value.Length <= Project.MaxDescriptionLength)
            .When(x => x.Description.HasValue)
            .WithMessage($"Project description cannot exceed {Project.MaxDescriptionLength} characters.");
    }
}