using Haven.Application.Common.Contracts;

using Version = Haven.Domain.ValueObjects.Version;

namespace Haven.Application.Common.Interfaces.Services.VersionCheck;

public interface IHavenVersionService
{
    /// <summary>
    /// The currently running version, or <see langword="null"/> when running a nightly build (see <see cref="IsNightly"/>).
    /// </summary>
    Version? CurrentVersion { get; }

    /// <summary>
    /// True when the running build is a nightly build, which is always considered up to date.
    /// </summary>
    bool IsNightly { get; }

    HavenReleaseInfo? LatestRelease { get; }
    Task<Result<HavenReleaseInfo>> GetLatestReleaseAsync(bool forceRefresh = false, CancellationToken ct = default);
}