using Haven.Application.Features.Backups.Commands.CreateBackup;

using Mediator;

using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.BackgroundJobs;

public sealed class BackupBackgroundJob(
    ISender mediator,
    ILogger<BackupBackgroundJob> logger)
{
    public async Task ExecuteAsync()
    {
        logger.LogInformation("Running scheduled backup");

        var result = await mediator.Send(new CreateBackupCommand());

        if (result.IsSuccess)
        {
            logger.LogInformation("Scheduled backup completed: {Path}", result.Value!.SnapshotPath);
            return;
        }

        logger.LogError("Scheduled backup failed: {Error}", result.Error);

        // Throw so Hangfire marks the job failed and retries it; returning normally would
        // silently skip the day's backup (e.g. when a manifest resync briefly held the lock).
        throw new InvalidOperationException($"Scheduled backup failed: {result.Error}");
    }
}