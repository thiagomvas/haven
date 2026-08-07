using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Common.Messaging;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;

namespace Haven.Application.Features.HealthChecks.Commands.DeleteHealthCheckCommand;

public class DeleteHealthCheckHandler(
    IHealthCheckRepository healthCheckRepository,
    IServiceRepository serviceRepository,
    IHealthCheckScheduler healthCheckScheduler)
    : ICommandHandler<DeleteHealthCheckCommand>
{
    public async ValueTask<Result> Handle(DeleteHealthCheckCommand command, CancellationToken cancellationToken)
    {
        var healthCheck = await healthCheckRepository.GetByIdAsync(command.HealthCheckId, cancellationToken);
        if (healthCheck is null)
            return Error.NotFoundFor(nameof(HealthCheck), command.HealthCheckId);

        var service = await serviceRepository.GetByIdAsync(healthCheck.ServiceId, cancellationToken);
        if (service is null)
            return Error.NotFoundFor(nameof(Service), healthCheck.ServiceId);

        service.RemoveHealthCheck(healthCheck);
        await healthCheckRepository.RemoveAsync(healthCheck, cancellationToken);

        healthCheckScheduler.Unschedule(healthCheck.Id);

        return Result.Success();
    }
}