using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Services;

using Version = Haven.Domain.ValueObjects.Version;

namespace Haven.Infrastructure.Services;

public class HavenVersionService(IGithubReleaseClient githubReleaseClient) : IHavenVersionService
{
    public Version CurrentVersion { get; } =
        Version.Parse(Environment.GetEnvironmentVariable("HAVEN_VERSION") ?? "0.0.0");

    public Version LatestVersion { get; private set; } = null!;

    public async Task<Result<Version>> GetLatestVersionAsync(bool forceRefresh = false, CancellationToken ct = default)
    {
        if (!forceRefresh && LatestVersion != null)
            return LatestVersion;

        var releasesResult = await githubReleaseClient.GetReleasesAsync(ct);
        if (releasesResult.IsFailure)
            return releasesResult.Error;

        var latest = releasesResult.Value
            .Where(r => !r.Draft && Version.TryParse(r.TagName, out _))
            .Select(r => Version.Parse(r.TagName))
            .OrderDescending()
            .FirstOrDefault();

        if (latest is null)
            return Error.NotFound;

        LatestVersion = latest;
        return LatestVersion;
    }
}
