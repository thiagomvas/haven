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
            .NotEmpty()
            .WithMessage("Environment alias is required.")
            .MaximumLength(8)
            .WithMessage("Environment alias cannot exceed 8 characters.")
            .Matches(@"^[a-z0-9][a-z0-9-]*[a-z0-9]$")
            .WithMessage("Environment alias may only contain lowercase letters, digits, and hyphens, and cannot start or end with a hyphen.");
    }
}
