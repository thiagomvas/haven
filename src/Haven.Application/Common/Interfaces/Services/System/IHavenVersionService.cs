using Version = Haven.Domain.ValueObjects.Version;

namespace Haven.Application.Common.Interfaces.Services;

public interface IHavenVersionService
{
    Version CurrentVersion { get; }
    Version LatestVersion { get; }
    Task<Result<Version>> GetLatestVersionAsync(bool forceRefresh = false, CancellationToken ct = default);
}