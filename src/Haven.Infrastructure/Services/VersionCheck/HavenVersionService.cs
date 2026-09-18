using Haven.Application.Common;
using Haven.Application.Common.Contracts;
using Haven.Application.Common.Interfaces.Services.VersionCheck;

using Version = Haven.Domain.ValueObjects.Version;

namespace Haven.Infrastructure.Services.VersionCheck;

public class HavenVersionService(IGithubReleaseClient githubReleaseClient) : IHavenVersionService
{
    private const string NightlyVersionLabel = "nightly";

    public Version? CurrentVersion { get; } = ParseCurrentVersion();

    public bool IsNightly { get; } = IsNightlyBuild();

    public HavenReleaseInfo? LatestRelease { get; private set; }

    public async Task<Result<HavenReleaseInfo>> GetLatestReleaseAsync(bool forceRefresh = false, CancellationToken ct = default)
    {
        if (!forceRefresh && LatestRelease is not null)
            return LatestRelease;

        var releasesResult = await githubReleaseClient.GetReleasesAsync(ct);
        if (releasesResult.IsFailure)
            return releasesResult.Error;

        var latest = releasesResult.Value
            .Where(r => !r.Draft && Version.TryParse(r.TagName, out _))
            .Select(r => new HavenReleaseInfo(
                Version.Parse(r.TagName),
                r.Name,
                r.HtmlUrl,
                r.Body,
                r.Prerelease,
                r.PublishedAt))
            .OrderByDescending(r => r.Version)
            .FirstOrDefault();

        if (latest is null)
            return Error.NotFound;

        LatestRelease = latest;
        return LatestRelease;
    }

    private static bool IsNightlyBuild() =>
        string.Equals(
            Environment.GetEnvironmentVariable("HAVEN_VERSION"),
            NightlyVersionLabel,
            StringComparison.OrdinalIgnoreCase);

    private static Version? ParseCurrentVersion()
    {
        var raw = Environment.GetEnvironmentVariable("HAVEN_VERSION") ?? "0.0.0";
        return string.Equals(raw, NightlyVersionLabel, StringComparison.OrdinalIgnoreCase)
            ? null
            : Version.Parse(raw);
    }
}