using FluentValidation;

namespace Haven.Application.Features.EnvironmentVariables.Commands.SetEnvForService;

public class SetEnvForServiceValidator : AbstractValidator<SetEnvForServiceCommand>
{
    public SetEnvForServiceValidator()
    {
        RuleFor(x => x.EnvironmentId)
            .NotEmpty()
            .WithMessage("Environment id cannot be empty");

        RuleFor(x => x.ServiceId)
            .NotEmpty()
            .WithMessage("Service id cannot be empty");

        RuleFor(x => x.EnvFile)
            .NotEmpty()
            .WithMessage("Env file cannot be empty");
    }
}