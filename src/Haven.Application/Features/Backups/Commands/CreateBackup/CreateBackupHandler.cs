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

        return Result<CreateBackupResult>.CreatedFor(new CreateBackupResult(snapshotPath, timestamp));
    }
}
