using Haven.Application.Features.Backups.Commands.RestoreBackup;

namespace Haven.Application.Common.Interfaces;

public interface IBackupManifestReader
{
    /// <summary>
    /// Resolves a restore source into a local directory path from which manifest files can be read.
    /// For FileSystem: validates and returns the snapshot directory directly.
    /// For Git: extracts the commit tree into a temporary directory and returns that path.
    /// </summary>
    Task<string> PrepareSourceDirectoryAsync(
        RestoreSource source,
        string? snapshotName,
        string? commitSha,
        CancellationToken ct);
}
