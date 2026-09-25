using Haven.Domain.Entities;

namespace Haven.Application.Common.Interfaces.Deployment;

public interface IContainerEnvironmentService
{
    Task<IEnumerable<EnvironmentVariables>> BuildEnvironmentVariablesAsync(Guid serviceId, CancellationToken cancellationToken = default);
}