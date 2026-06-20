using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Application.Configuration;

namespace Haven.Application.Features.Backups.Queries.GetBackupOptions;

[RequirePermission(Permissions.System.ManageBackups)]
public sealed record GetBackupOptionsQuery : IQuery<BackupOptions>;