using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Backups.Commands.RestoreBackup;

public enum RestoreSource { FileSystem, Git, Manifest }

[RequirePermission(Permissions.System.ManageBackups)]
public sealed class RestoreBackupCommand : ICommand<RestoreBackupResult>
{
    public RestoreSource Source { get; set; }
    public string? SnapshotName { get; set; }
    public string? CommitSha { get; set; }
    public bool DryRun { get; set; } = false;
}
