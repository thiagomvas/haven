using FluentValidation;

using Haven.Application.Extensions;

namespace Haven.Application.Features.HealthChecks.Commands.TestHealthCheckCommand;

public class TestHealthCheckValidator : AbstractValidator<TestHealthCheckCommand>
{
    public TestHealthCheckValidator()
    {
        RuleFor(x => x.ServiceId).ValidId();
        RuleFor(x => x.Kind)
            .IsInEnum()
            .WithMessage("Kind must be a valid health check kind.");
        RuleFor(x => x.Config)
            .Must((command, config) => HealthCheckConfigValidator.IsValid(command.Kind, config))
            .WithMessage("Config is not valid for the selected health check kind.");
    }
}
