using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Common.Messaging;
using Haven.Domain.Entities;

namespace Haven.Application.Features.HealthChecks.Commands.RunHealthCheckNowCommand;

public class RunHealthCheckNowHandler(
    IHealthCheckRepository healthCheckRepository,
    IHealthCheckScheduler healthCheckScheduler)
    : ICommandHandler<RunHealthCheckNowCommand>
{
    public async ValueTask<Result> Handle(RunHealthCheckNowCommand command, CancellationToken cancellationToken)
    {
        var healthCheck = await healthCheckRepository.GetByIdAsync(command.HealthCheckId, cancellationToken);
        if (healthCheck is null)
            return Error.NotFoundFor(nameof(HealthCheck), command.HealthCheckId);

        healthCheckScheduler.RunNow(healthCheck.Id);

        return Result.Success();
    }
}
