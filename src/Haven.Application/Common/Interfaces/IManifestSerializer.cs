using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Application.Common.Interfaces;

public interface IManifestSerializer<T>
{
    Task WriteAsync(T item, CancellationToken ct = default);
    Task RenameAsync(T item, string oldName, string newName, CancellationToken ct = default);
    Task<IReadOnlyList<T>> ReadAsync(Guid parentId = default, CancellationToken ct = default);
    Task RemoveAsync(T item, CancellationToken ct = default);
}

public interface IManifestSerializer
{
    Task WriteProjectAsync(Project project, CancellationToken ct);
    Task DeleteProjectAsync(Project project, CancellationToken ct);
    Task RenameProjectAsync(string oldProjectName, string newProjectName, CancellationToken ct);
    Task<IReadOnlyList<Project>> ReadProjectsAsync(CancellationToken ct);

    Task WriteEnvironmentAsync(Project project, Environment environment, CancellationToken ct);
    Task DeleteEnvironmentAsync(Project project, string environmentName, CancellationToken ct);
    Task RenameEnvironmentAsync(Project project, string oldEnvironmentName, string newEnvironmentName, CancellationToken ct);

    Task WriteServiceAsync(Project project, Environment environment, Service service, CancellationToken ct);
    Task DeleteServiceAsync(Project project, Environment environment, string serviceName, CancellationToken ct);
    Task RenameServiceAsync(Project project, Environment environment, string oldServiceName, string newServiceName, CancellationToken ct);

    Task WriteNetworkAsync(Project project, Environment environment, Network network, CancellationToken ct);
    Task DeleteNetworkAsync(Project project, Environment environment, CancellationToken ct);
}