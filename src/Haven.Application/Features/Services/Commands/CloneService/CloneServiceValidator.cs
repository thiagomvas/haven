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
            .MaximumLength(Project.MaxAliasLength)
            .WithMessage($"Service alias cannot exceed {Project.MaxAliasLength} characters.")
            .When(x => !string.IsNullOrEmpty(x.NewAlias));
    }
}
