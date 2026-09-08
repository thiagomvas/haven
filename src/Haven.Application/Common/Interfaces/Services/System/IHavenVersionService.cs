using Haven.Application.Common.Contracts;

using Version = Haven.Domain.ValueObjects.Version;

namespace Haven.Application.Common.Interfaces.Services;

public interface IHavenVersionService
{
    Version CurrentVersion { get; }
    HavenReleaseInfo? LatestRelease { get; }
    Task<Result<HavenReleaseInfo>> GetLatestReleaseAsync(bool forceRefresh = false, CancellationToken ct = default);
}
