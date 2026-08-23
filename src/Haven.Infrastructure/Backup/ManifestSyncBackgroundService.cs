using Haven.Application.Common.Interfaces;
using Haven.Application.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Haven.Infrastructure.Backup;

/// <summary>
/// The sole writer of the live manifests directory. Every mutating command that implements
/// <c>IMutatesManifestState</c> requests a sync via <see cref="ManifestSyncTrigger"/> after it commits;
/// this service debounces those requests and performs one full <see cref="IBackupManifestWriter.WriteAllAsync"/>
/// resync per quiet period, replacing the old per-entity domain-event handlers (which had gaps - e.g.
/// Sidecar updates/deletes and Network updates were never written - and could drift from the writer's
/// own full re-dump used by backup/restore).
/// </summary>
public sealed class ManifestSyncBackgroundService(
    ManifestSyncTrigger trigger,
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<ManifestsOptions> manifestsOptions,
    IBackupCoordinationLock coordinationLock,
    ILogger<ManifestSyncBackgroundService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (await trigger.Reader.WaitToReadAsync(stoppingToken))
            {
                while (trigger.Reader.TryRead(out _)) { }

                var debounce = TimeSpan.FromSeconds(Math.Max(1, manifestsOptions.CurrentValue.SyncDebounceSeconds));
                await Task.Delay(debounce, stoppingToken);

                // A trailing mutation may have arrived during the delay; drain again so it's
                // included in this write instead of triggering an immediate second one right after.
                while (trigger.Reader.TryRead(out _)) { }

                await WriteOnceAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Expected on host shutdown.
        }
    }

    private async Task WriteOnceAsync(CancellationToken ct)
    {
        if (!coordinationLock.TryAcquire(out var release))
        {
            // A manual backup/restore is in flight; it will leave manifests consistent on its own,
            // and the mutation that triggered this pass is retried by re-requesting sync.
            logger.LogDebug("Skipping debounced manifest resync: another backup operation is in progress");
            trigger.RequestSync();
            return;
        }

        using var _ = release;
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var writer = scope.ServiceProvider.GetRequiredService<IBackupManifestWriter>();
            await writer.WriteAllAsync(manifestsOptions.CurrentValue.ManifestsPath, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Debounced manifest resync failed");
        }
    }
}