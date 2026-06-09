namespace Haven.Application.Common.Interfaces;

public interface IManifestSyncService
{
    Task SyncAsync(CancellationToken ct = default);
}