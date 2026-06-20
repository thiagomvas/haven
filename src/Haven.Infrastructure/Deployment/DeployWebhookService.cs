using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Entities;

namespace Haven.Infrastructure.Deployment;

public class DeployWebhookService(IDeploymentJobEnqueuer jobEnqueuer, IServiceRepository repository) : IDeployWebhookService
{
    public async Task<Result> TryEnqueueWithTokenAsync(string token, CancellationToken ct)
    {
        var service = await repository.GetByTokenAsync(token, ct);
        if (service?.Environment is null) return Error.NotFound;

        jobEnqueuer.EnqueueDeployment(service.Environment!.ProjectId, service.EnvironmentId, service.Id);
        return Result.Success();
    }
}