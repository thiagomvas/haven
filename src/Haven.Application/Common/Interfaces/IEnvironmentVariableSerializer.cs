namespace Haven.Application.Common.Interfaces;

public interface IEnvironmentVariableSerializer
{
    Task WriteExampleForProjectAsync(Guid projectId, CancellationToken cancellationToken);
    Task WriteExampleForEnvironmentAsync(Guid environmentId, CancellationToken cancellationToken);
    Task WriteExampleForServiceAsync(Guid serviceId, CancellationToken cancellationToken);

    Task ReadAndSyncExampleForProjectAsync(Guid projectId, CancellationToken cancellationToken);
    Task ReadAndSyncExampleForEnvironmentAsync(Guid environmentId, CancellationToken cancellationToken);
    Task ReadAndSyncExampleForServiceAsync(Guid serviceId, CancellationToken cancellationToken);
}