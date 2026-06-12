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
            logger.LogInformation("Scheduled backup completed: {Path}", result.Value!.SnapshotPath);
        else
            logger.LogError("Scheduled backup failed: {Error}", result.Error);
    }
}