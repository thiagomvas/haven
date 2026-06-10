using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Backups.Commands.CreateBackup;

[RequirePermission(Permissions.System.ManageBackups)]
public sealed class CreateBackupCommand : ICommand<CreateBackupResult>;
