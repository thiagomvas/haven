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
            .MaximumLength(Project.MaxNameLength)
            .WithMessage($"Project name cannot exceed {Project.MaxNameLength} characters.");

        RuleFor(x => x.Description)
            .MaximumLength(Project.MaxDescriptionLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Description))
            .WithMessage($"Project description cannot exceed {Project.MaxDescriptionLength} characters.");
    }
}