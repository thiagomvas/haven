using FluentValidation;

namespace Haven.Application.Features.EnvironmentVariables.Commands.SetEnvForProject;

public class SetEnvForProjectValidator : AbstractValidator<SetEnvForProjectCommand>
{
    public SetEnvForProjectValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty()
            .WithMessage("Project id cannot be empty");

        RuleFor(x => x.EnvFile)
            .NotEmpty()
            .WithMessage("Env file cannot be empty");
    }

}