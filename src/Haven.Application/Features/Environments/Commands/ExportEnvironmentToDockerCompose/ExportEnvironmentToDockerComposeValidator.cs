using FluentValidation;

namespace Haven.Application.Features.Environments.Commands.ExportEnvironmentToDockerCompose;

public sealed class ExportEnvironmentToDockerComposeValidator : AbstractValidator<ExportEnvironmentToDockerComposeCommand>
{
    public ExportEnvironmentToDockerComposeValidator()
    {
        RuleFor(x => x.EnvironmentId)
            .NotEmpty()
            .WithMessage("Environment ID cannot be empty.");
    }
}
