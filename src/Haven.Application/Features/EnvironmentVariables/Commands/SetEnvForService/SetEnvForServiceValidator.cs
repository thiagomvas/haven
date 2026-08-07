using FluentValidation;

using Haven.Application.Features.EnvironmentVariables.Commands.SetEnvForEnvironment;

namespace Haven.Application.Features.EnvironmentVariables.Commands.SetEnvForService;

public class SetEnvForServiceValidator : AbstractValidator<SetEnvForEnvironmentCommand>
{
    public SetEnvForServiceValidator()
    {
        RuleFor(x => x.EnvironmentId)
            .NotEmpty()
            .WithMessage("Environment id cannot be empty");

        RuleFor(x => x.EnvFile)
            .NotEmpty()
            .WithMessage("Env file cannot be empty");
    }
}