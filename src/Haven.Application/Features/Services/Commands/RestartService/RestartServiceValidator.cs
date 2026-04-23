using FluentValidation;

namespace Haven.Application.Features.Services.Commands.RestartService;

public class RestartServiceValidator : AbstractValidator<RestartServiceCommand>
{
    public RestartServiceValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty().WithMessage("Project ID cannot be empty.");
        RuleFor(x => x.EnvironmentId).NotEmpty().WithMessage("Environment ID cannot be empty.");
        RuleFor(x => x.ServiceId).NotEmpty().WithMessage("Service ID cannot be empty.");
    }
}
