using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.Backups.Commands.RestoreBackup;

namespace Haven.Application.Features.Manifests.Commands.SyncFromManifests;

[AdminOnly]
public sealed record SyncFromManifestsCommand(bool DryRun = false) : ICommand<RestoreBackupResult>;
