using Haven.Application.Common.Interfaces.Services;

using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.BackgroundJobs;

public sealed class HavenVersionCheckJob(
    IHavenVersionService versionService,
    ILogger<HavenVersionCheckJob> logger)
{
    public async Task ExecuteAsync()
    {
        var result = await versionService.GetLatestVersionAsync(forceRefresh: true);
        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to check for the latest Haven release: {Error}",
                result.Error.Message);
            return;
        }

        var latest = result.Value;
        var current = versionService.CurrentVersion;

        if (latest > current)
        {
            logger.LogWarning(
                "A newer Haven release is available: {LatestVersion} (current: {CurrentVersion})",
                latest, current);
        }
        else
        {
            logger.LogInformation(
                "Haven is up to date. Latest release: {LatestVersion}, current: {CurrentVersion}",
                latest, current);
        }
    }
}
