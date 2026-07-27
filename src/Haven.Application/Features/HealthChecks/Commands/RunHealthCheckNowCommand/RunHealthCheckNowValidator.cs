using FluentValidation;

using Haven.Application.Extensions;

namespace Haven.Application.Features.HealthChecks.Commands.RunHealthCheckNowCommand;

public class RunHealthCheckNowValidator : AbstractValidator<RunHealthCheckNowCommand>
{
    public RunHealthCheckNowValidator()
    {
        RuleFor(x => x.HealthCheckId).ValidId();
    }
}
