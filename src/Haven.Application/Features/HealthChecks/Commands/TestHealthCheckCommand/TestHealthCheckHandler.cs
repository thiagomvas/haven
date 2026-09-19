using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Common.Messaging;
using Haven.Application.Mappers;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;

namespace Haven.Application.Features.HealthChecks.Commands.TestHealthCheckCommand;

public sealed class TestHealthCheckHandler(
    IServiceRepository serviceRepository,
    IHealthCheckExecutor healthCheckExecutor)
    : ICommandHandler<TestHealthCheckCommand, HealthCheckResultDto>
{
    public async ValueTask<Result<HealthCheckResultDto>> Handle(TestHealthCheckCommand command, CancellationToken cancellationToken)
    {
        var service = await serviceRepository.GetByIdAsync(command.ServiceId, cancellationToken);
        if (service is null)
            return Error.NotFoundFor(nameof(Service), command.ServiceId);

        var transient = HealthCheck.Create(service.Id, "test", command.Kind, enabled: true, cronExpression: null, command.Config);

        var result = await healthCheckExecutor.TestAsync(transient, cancellationToken);
        return result.ToDto(DateTime.UtcNow);
    }
}
