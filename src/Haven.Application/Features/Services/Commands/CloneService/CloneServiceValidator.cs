using FluentValidation;

using Haven.Domain.Aggregates;

namespace Haven.Application.Features.Services.Commands.CloneService;

public sealed class CloneServiceValidator : AbstractValidator<CloneServiceCommand>
{
    public CloneServiceValidator()
    {
        RuleFor(x => x.NewName)
            .NotEmpty()
            .WithMessage("Service name cannot be empty.");

        RuleFor(x => x.NewAlias)
            .NotEmpty()
            .WithMessage("Service alias is required.")
            .MaximumLength(Project.MaxAliasLength)
            .WithMessage($"Service alias cannot exceed {Project.MaxAliasLength} characters.");
    }
}
