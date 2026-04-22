using FluentValidation;

namespace Haven.Application.Features.Services.Commands.StopService;

public class StopServiceValidator : AbstractValidator<StopServiceCommand>
{
    public StopServiceValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty()
            .WithMessage("Project ID cannot be empty.");

        RuleFor(x => x.EnvironmentId)
            .NotEmpty()
            .WithMessage("Environment ID cannot be empty.");

        RuleFor(x => x.ServiceId)
            .NotEmpty()
            .WithMessage("Service ID cannot be empty.");
    }
}