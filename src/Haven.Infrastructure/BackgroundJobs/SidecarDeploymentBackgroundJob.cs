using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;

using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.BackgroundJobs;

public sealed class SidecarDeploymentBackgroundJob(
    ISidecarRepository sidecarRepository,
    IDeploymentOrchestrator orchestrator,
    IUnitOfWork unitOfWork,
    ILogger<SidecarDeploymentBackgroundJob> logger)
{
    public async Task<Result> ExecuteOperationAsync(Guid sidecarId, ServiceJobOperation operation)
    {
        logger.LogInformation("Starting {Operation} for sidecar {SidecarId}", operation, sidecarId);

        var sidecar = await sidecarRepository.GetByIdAsync(sidecarId, CancellationToken.None);
        if (sidecar is null)
        {
            logger.LogError("Sidecar {SidecarId} not found during {Operation} execution", sidecarId, operation);
            return Result.Failure(Error.NotFoundFor("Sidecar", sidecarId));
        }

        var result = operation switch
        {
            ServiceJobOperation.Deploy => await orchestrator.DeployAsync(sidecar, CancellationToken.None),
            ServiceJobOperation.Stop => await orchestrator.StopAsync(sidecar, CancellationToken.None),
            _ => Result.Failure(Error.NotSupported)
        };

        if (result.IsSuccess)
            logger.LogInformation("{Operation} succeeded for sidecar {SidecarId}", operation, sidecarId);
        else
            logger.LogError("{Operation} failed for sidecar {SidecarId}: {Error}", operation, sidecarId, result.Error);

        await unitOfWork.SaveChangesAsync(CancellationToken.None);
        return result;
    }
}
