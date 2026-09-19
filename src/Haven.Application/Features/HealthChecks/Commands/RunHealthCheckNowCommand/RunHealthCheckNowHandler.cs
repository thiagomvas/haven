using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Common.Messaging;
using Haven.Application.Mappers;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;

namespace Haven.Application.Features.HealthChecks.Commands.RunHealthCheckNowCommand;

public sealed class RunHealthCheckNowHandler(
    IHealthCheckRepository healthCheckRepository,
    IHealthCheckExecutor healthCheckExecutor)
    : ICommandHandler<RunHealthCheckNowCommand, HealthCheckResultDto>
{
    public async ValueTask<Result<HealthCheckResultDto>> Handle(RunHealthCheckNowCommand command, CancellationToken cancellationToken)
    {
        var healthCheck = await healthCheckRepository.GetByIdAsync(command.HealthCheckId, cancellationToken);
        if (healthCheck is null)
            return Error.NotFoundFor(nameof(HealthCheck), command.HealthCheckId);

        var result = await healthCheckExecutor.ExecuteAsync(healthCheck.Id, cancellationToken);
        if (result is null)
            return Error.NotFoundFor(nameof(Service), healthCheck.ServiceId);

        return result.ToDto(healthCheck.LastRunAt ?? DateTime.UtcNow);
    }
}
