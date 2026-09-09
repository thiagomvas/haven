using Haven.Application.Common.Contracts;

namespace Haven.Application.Common.Interfaces.Services.VersionCheck;

public interface IGithubReleaseClient
{
    Task<Result<IReadOnlyList<GithubRelease>>> GetReleasesAsync(CancellationToken ct = default);
}