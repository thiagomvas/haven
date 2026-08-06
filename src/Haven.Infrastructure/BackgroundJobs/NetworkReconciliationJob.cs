using Haven.Application.Common.Interfaces.Deployment;

using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.BackgroundJobs;

public sealed class NetworkReconciliationJob(
    INetworkReconciliationService reconciliationService,
    ILogger<NetworkReconciliationJob> logger)
{
    public async Task ExecuteAsync()
    {
        try
        {
            await reconciliationService.ReconcileAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Network reconciliation run failed; will retry on the next schedule");
        }
    }
}
