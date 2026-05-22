using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Domain.Entities;
using Haven.Infrastructure.Persistence;

namespace Haven.Infrastructure.Deployment;

public class DeploymentOrchestrator(HavenDbContext dbContext, IDeployServiceFactory deployServiceFactory) : IDeploymentOrchestrator
{
    public async Task<Result> DeployServiceAsync(Service service, CancellationToken cancellationToken)
    {
        if (service is null) return Error.NotFound;
        if (service.Environment?.Project is null) return Error.NotFound;

        service.MarkDeploying();
        await dbContext.SaveChangesAsync(cancellationToken);

        var deployService = deployServiceFactory.Create(service);
        if (deployService is null)
            return Error.Failure("Deploy.NotSupported",
                "No deployment service available for the specified service type.");
        var deployResult = await deployService.DeployAsync(service, cancellationToken);

        if (deployResult.IsFailure)
            service.MarkStopped();
        else service.MarkDeployed();

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}