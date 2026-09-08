using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Common.Interfaces.Services.System;

using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.BackgroundJobs;

public sealed class HavenVersionCheckJob(
    IHavenVersionService versionService,
    ILogger<HavenVersionCheckJob> logger)
{
    public async Task ExecuteAsync()
    {
        var result = await versionService.GetLatestReleaseAsync(forceRefresh: true);
        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to check for the latest Haven release: {Error}",
                result.Error.Message);
            return;
        }

        var latest = result.Value;
        var current = versionService.CurrentVersion;

        if (latest.Version > current)
        {
            logger.LogWarning(
                "A newer Haven release is available: {LatestVersion} '{ReleaseName}' (current: {CurrentVersion}) - {ReleaseUrl}",
                latest.Version, latest.Name, current, latest.HtmlUrl);
        }
        else
        {
            logger.LogInformation(
                "Haven is up to date. Latest release: {LatestVersion} '{ReleaseName}', current: {CurrentVersion}",
                latest.Version, latest.Name, current);
        }
    }
}