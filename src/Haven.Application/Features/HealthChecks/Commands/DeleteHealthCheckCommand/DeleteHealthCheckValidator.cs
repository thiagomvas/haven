using FluentValidation;

using Haven.Application.Extensions;

namespace Haven.Application.Features.HealthChecks.Commands.DeleteHealthCheckCommand;

public class DeleteHealthCheckValidator : AbstractValidator<DeleteHealthCheckCommand>
{
    public DeleteHealthCheckValidator()
    {
        RuleFor(x => x.HealthCheckId).ValidId();
    }
}
