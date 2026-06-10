using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Application.Configuration;

namespace Haven.Application.Features.Backups.Commands.UpdateBackupOptions;

[RequirePermission(Permissions.System.ManageBackups)]
public sealed record UpdateBackupOptionsCommand(BackupOptions Options) : ICommand<BackupOptions>;
