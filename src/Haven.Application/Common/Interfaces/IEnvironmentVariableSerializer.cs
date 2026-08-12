using Haven.Domain.Entities;

namespace Haven.Application.Common.Interfaces;

public interface IEnvironmentVariableSerializer
{
    Task<Result> WriteExampleForProjectAsync(Guid projectId, CancellationToken cancellationToken);
    Task<Result> WriteExampleForEnvironmentAsync(Guid environmentId, CancellationToken cancellationToken);
    Task<Result> WriteExampleForServiceAsync(Guid serviceId, CancellationToken cancellationToken);

    Task<Result> ReadAndSyncExampleForProjectAsync(Guid projectId, CancellationToken cancellationToken);
    Task<Result> ReadAndSyncExampleForEnvironmentAsync(Guid environmentId, CancellationToken cancellationToken);
    Task<Result> ReadAndSyncExampleForServiceAsync(Guid serviceId, CancellationToken cancellationToken);

    string Serialize(IEnumerable<EnvironmentVariables> variables, bool includeValues = true);
}