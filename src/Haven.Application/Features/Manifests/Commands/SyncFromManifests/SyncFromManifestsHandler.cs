using Haven.Application.Common;
using Haven.Application.Features.Backups.Commands.RestoreBackup;

using Mediator;

namespace Haven.Application.Features.Manifests.Commands.SyncFromManifests;

/// <summary>
/// "Sync from manifests" is a restore from the live manifests directory: delegating to
/// <see cref="RestoreBackupCommand"/> with <see cref="RestoreSource.Manifest"/> gives it the same
/// ID-based diffing, dry-run support, atomic manifest rewrite, and deployment cleanup as a regular
/// backup restore, instead of the destructive full wipe-and-recreate this used to do.
/// </summary>
public sealed class SyncFromManifestsHandler(IMediator sender)
    : Common.Messaging.ICommandHandler<SyncFromManifestsCommand, RestoreBackupResult>
{
    public async ValueTask<Result<RestoreBackupResult>> Handle(SyncFromManifestsCommand request, CancellationToken cancellationToken)
        => await sender.Send(new RestoreBackupCommand
        {
            Source = RestoreSource.Manifest,
            DryRun = request.DryRun
        }, cancellationToken);
}
