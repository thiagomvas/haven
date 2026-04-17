using Haven.Domain.Aggregates;
using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Application.Common.Interfaces;

public interface IManifestSerializer
{
    Task WriteProjectAsync(Project project, CancellationToken ct);
    Task DeleteProjectAsync(Project project, CancellationToken ct);
    Task<IReadOnlyList<Project>> ReadProjectsAsync(CancellationToken ct);

    Task WriteEnvironmentAsync(Project project, Environment environment, CancellationToken ct);
    Task DeleteEnvironmentAsync(Project project, string environmentName, CancellationToken ct);
}