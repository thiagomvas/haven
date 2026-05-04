using Haven.Domain.Entities;

namespace Haven.Application.Common.Interfaces.Repositories;

public interface IEnvironmentVariableRepository
{
    Task<IEnumerable<EnvironmentVariables>> GetForServiceAsync(Guid serviceId, CancellationToken cancellationToken);
    Task<IEnumerable<EnvironmentVariables>> GetForEnvironmentAsync(Guid environmentId, CancellationToken cancellationToken);
    Task<IEnumerable<EnvironmentVariables>> GetForProjectAsync(Guid projectId, CancellationToken cancellationToken);
    Task AddAsync(EnvironmentVariables environmentVariable, CancellationToken cancellationToken);
    Task AddAsync(IEnumerable<EnvironmentVariables> environmentVariables, CancellationToken cancellationToken);
    Task RemoveAsync(EnvironmentVariables environmentVariable, CancellationToken cancellationToken);
    Task CleanForProjectAsync(Guid projectId, CancellationToken cancellationToken);
    Task CleanForServiceAsync(Guid serviceId, CancellationToken cancellationToken);
    Task CleanForEnvironmentAsync(Guid environmentId, CancellationToken cancellationToken);
}