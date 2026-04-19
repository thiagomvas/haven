using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Application.Common.Interfaces;

public interface IManifestSerializer
{
    Task WriteProjectAsync(Project project, CancellationToken ct);
    Task DeleteProjectAsync(Project project, CancellationToken ct);
    Task<IReadOnlyList<Project>> ReadProjectsAsync(CancellationToken ct);

    Task WriteEnvironmentAsync(Project project, Environment environment, CancellationToken ct);
    Task DeleteEnvironmentAsync(Project project, string environmentName, CancellationToken ct);

    Task WriteServiceAsync(Project project, Environment environment, Service service, CancellationToken ct);
    Task DeleteServiceAsync(Project project, Environment environment, string serviceName, CancellationToken ct);
}