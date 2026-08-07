using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Common.Messaging;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;

namespace Haven.Application.Features.HealthChecks.Commands.CreateHealthCheckCommand;

public class CreateHealthCheckHandler(
    IServiceRepository serviceRepository,
    IHealthCheckRepository healthCheckRepository,
    IHealthCheckScheduler healthCheckScheduler)
    : ICommandHandler<CreateHealthCheckCommand, Guid>
{
    public async ValueTask<Result<Guid>> Handle(CreateHealthCheckCommand command, CancellationToken cancellationToken)
    {
        var service = await serviceRepository.GetByIdAsync(command.ServiceId, cancellationToken);
        if (service is null)
            return Error.NotFoundFor(nameof(Service), command.ServiceId);

        var healthCheck = service.AddHealthCheck(
            command.Name,
            command.Kind,
            command.Enabled,
            command.CronExpression,
            command.Config);

        await healthCheckRepository.AddAsync(healthCheck, cancellationToken);

        healthCheckScheduler.Schedule(healthCheck);

        return Result<Guid>.CreatedFor(healthCheck.Id);
    }
}