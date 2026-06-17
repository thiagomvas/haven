using FluentValidation;

using DomainEnvironment = Haven.Domain.Entities.Environment;

namespace Haven.Application.Features.Environments.Commands.CloneEnvironment;

public sealed class CloneEnvironmentValidator : AbstractValidator<CloneEnvironmentCommand>
{
    public CloneEnvironmentValidator()
    {
        RuleFor(x => x.NewName)
            .NotEmpty()
            .WithMessage("Environment name cannot be empty.")
            .MaximumLength(DomainEnvironment.MaxNameLength)
            .WithMessage($"Environment name cannot exceed {DomainEnvironment.MaxNameLength} characters.");

        RuleFor(x => x.NewAlias)
            .MaximumLength(8)
            .WithMessage("Environment alias cannot exceed 8 characters.")
            .When(x => !string.IsNullOrEmpty(x.NewAlias));
    }
}
