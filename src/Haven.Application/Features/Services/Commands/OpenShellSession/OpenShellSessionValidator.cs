using FluentValidation;

namespace Haven.Application.Features.Services.Commands.OpenShellSession;

public class OpenShellSessionValidator : AbstractValidator<OpenShellSessionCommand>
{
    public OpenShellSessionValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty().WithMessage("Project ID cannot be empty.");
        RuleFor(x => x.EnvironmentId).NotEmpty().WithMessage("Environment ID cannot be empty.");
        RuleFor(x => x.ServiceId).NotEmpty().WithMessage("Service ID cannot be empty.");
        RuleFor(x => x.ShellType).IsInEnum();
    }
}
