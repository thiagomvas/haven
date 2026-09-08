using Haven.Application.Common.Contracts;

namespace Haven.Application.Common.Interfaces.Services;

public interface IGithubReleaseClient
{
    Task<Result<IReadOnlyList<GithubRelease>>> GetReleasesAsync(CancellationToken ct = default);
}
