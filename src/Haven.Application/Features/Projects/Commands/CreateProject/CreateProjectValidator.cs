using FluentValidation;

namespace Haven.Application.Features.Projects.Commands.CreateProject;

public sealed class CreateProjectValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required.");
        
        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description cannot exceed 500 characters.");
    }
}