using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Manifests.Commands.SyncFromManifests;

public sealed class SyncFromManifestsHandler(IManifestSyncService syncService)
    : ICommandHandler<SyncFromManifestsCommand>
{
    public async ValueTask<Result> Handle(SyncFromManifestsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await syncService.SyncAsync(cancellationToken);
            return Result.Success();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Error.ManifestSyncFailed;
        }
    }
}