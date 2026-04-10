using Haven.Domain.Aggregates;

namespace Haven.Application.Common.Interfaces;

public interface IManifestSerializer
{
    Task WriteProjectAsync(Project project, CancellationToken ct);
}