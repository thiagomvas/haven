using Haven.Application.Features.Backups.Commands.RestoreBackup;

namespace Haven.Application.Common.Interfaces;

public interface IBackupManifestReader
{
    Task<string> PrepareSourceDirectoryAsync(
        RestoreSource source,
        string? snapshotName,
        string? commitSha,
        CancellationToken ct);
}
