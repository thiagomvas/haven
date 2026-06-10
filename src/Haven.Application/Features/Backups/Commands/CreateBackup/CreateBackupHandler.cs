using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Messaging;
using Haven.Application.Configuration;

using Microsoft.Extensions.Options;

namespace Haven.Application.Features.Backups.Commands.CreateBackup;

public sealed class CreateBackupHandler(
    IBackupManifestWriter backupManifestWriter,
    IOptionsMonitor<BackupOptions> backupOptions)
    : ICommandHandler<CreateBackupCommand, CreateBackupResult>
{
    public async ValueTask<Result<CreateBackupResult>> Handle(CreateBackupCommand request, CancellationToken cancellationToken)
    {
        var options = backupOptions.CurrentValue;
        var timestamp = DateTimeOffset.UtcNow;
        var snapshotPath = Path.Combine(options.BackupsPath, timestamp.ToString("yyyyMMdd-HHmmss"));

        await backupManifestWriter.WriteAllAsync(snapshotPath, cancellationToken);

        ApplyRetention(options);

        return Result<CreateBackupResult>.CreatedFor(new CreateBackupResult(snapshotPath, timestamp));
    }

    private static void ApplyRetention(BackupOptions options)
    {
        if (!Directory.Exists(options.BackupsPath))
            return;

        var snapshots = Directory.GetDirectories(options.BackupsPath)
            .OrderDescending()
            .ToList();

        foreach (var snapshot in snapshots.Skip(options.RetentionCount))
            Directory.Delete(snapshot, recursive: true);
    }
}
