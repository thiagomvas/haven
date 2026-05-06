using FluentValidation;

namespace Haven.Application.Features.EnvironmentVariables.Commands.SetEnvForEnvironment;

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