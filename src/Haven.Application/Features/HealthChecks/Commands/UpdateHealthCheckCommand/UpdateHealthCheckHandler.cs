using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Common.Messaging;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;

namespace Haven.Application.Features.HealthChecks.Commands.UpdateHealthCheckCommand;

public class UpdateHealthCheckHandler(
    IHealthCheckRepository healthCheckRepository,
    IServiceRepository serviceRepository,
    IHealthCheckScheduler healthCheckScheduler)
    : ICommandHandler<UpdateHealthCheckCommand>
{
    public async ValueTask<Result> Handle(UpdateHealthCheckCommand command, CancellationToken cancellationToken)
    {
        var healthCheck = await healthCheckRepository.GetByIdAsync(command.HealthCheckId, cancellationToken);
        if (healthCheck is null)
            return Error.NotFoundFor(nameof(HealthCheck), command.HealthCheckId);

        var service = await serviceRepository.GetByIdAsync(healthCheck.ServiceId, cancellationToken);
        if (service is null)
            return Error.NotFoundFor(nameof(Service), healthCheck.ServiceId);

        service.UpdateHealthCheck(
            healthCheck,
            command.Name,
            command.Enabled.ToOptional(),
            command.CronExpression,
            command.ClearCronExpression,
            command.Config);

        healthCheckScheduler.Schedule(healthCheck);

        return Result.Success();
    }
}