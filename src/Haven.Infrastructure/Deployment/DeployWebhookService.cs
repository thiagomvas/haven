using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Entities;

namespace Haven.Infrastructure.Deployment;

public class DeployWebhookService(IDeploymentJobEnqueuer jobEnqueuer, IServiceRepository repository, IDeploymentLogService deploymentLogService, IUnitOfWork unitOfWork) : IDeployWebhookService
{
    public async Task<Result> TryEnqueueWithTokenAsync(string token, CancellationToken ct)
    {
        var service = await repository.GetByTokenAsync(token, ct);
        if (service?.Environment is null) return Error.NotFound;

        await deploymentLogService.QueueDeploymentForServiceAsync(service.Id, ct);
        service.AnnounceDeploymentStarted();
        await unitOfWork.SaveChangesAsync(ct);
        await jobEnqueuer.EnqueueDeployment(service.Environment!.ProjectId, service.EnvironmentId, service.Id, ct);
        return Result.Success();
    }
}