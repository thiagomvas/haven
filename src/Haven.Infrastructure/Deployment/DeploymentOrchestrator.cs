using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Domain.Entities;
using Haven.Infrastructure.Persistence;

namespace Haven.Infrastructure.Deployment;

public class DeploymentOrchestrator(HavenDbContext dbContext, IDeployServiceFactory deployServiceFactory, IDeploymentLogService logService) : IDeploymentOrchestrator
{
    public async Task<Result> DeployServiceAsync(Service service, CancellationToken cancellationToken)
    {
        if (service is null) return Error.NotFound;
        if (service.Environment?.Project is null) return Error.NotFound;

        service.MarkDeploying();
        var deployment = await logService.CreateDeploymentForServiceAsync(service.Id, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var deployService = deployServiceFactory.Create(service);
        if (deployService is null)
            return Error.Failure("Deploy.NotSupported",
                "No deployment service available for the specified service type.");
        var deployResult = await deployService.DeployAsync(service, cancellationToken);

        if (deployResult.IsFailure)
        {
            service.MarkStopped();
            await logService.MarkDeploymentFailedAsync(deployment.Id, cancellationToken);
        }
        else
        {
            service.MarkDeployed();
            await logService.MarkDeploymentCompletedAsync(deployment.Id, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> StopServiceAsync(Service service, CancellationToken cancellationToken)
    {
        var deployService = deployServiceFactory.Create(service);
        if (deployService is null)
            return Error.Failure("Deploy.NotSupported",
                "No deployment service available for the specified service type.");

        var stopResult = await deployService.StopAsync(service, cancellationToken);
        if (stopResult.IsFailure)
            return stopResult;

        service.MarkStopped();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> StartServiceAsync(Service service, CancellationToken cancellationToken)
    {
        var deployService = deployServiceFactory.Create(service);
        if (deployService is null)
            return Error.Failure("Deploy.NotSupported",
                "No deployment service available for the specified service type.");

        service.MarkDeploying();
        await dbContext.SaveChangesAsync(cancellationToken);

        var startResult = await deployService.StartAsync(service, cancellationToken);
        if (startResult.IsFailure)
        {
            service.MarkStopped();
        }
        else
        {
            service.MarkDeployed();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> RestartServiceAsync(Service service, CancellationToken cancellationToken)
    {
        var deployService = deployServiceFactory.Create(service);
        if (deployService is null)
            return Error.Failure("Deploy.NotSupported",
                "No deployment service available for the specified service type.");

        service.MarkDeploying();
        await dbContext.SaveChangesAsync(cancellationToken);

        var stopResult = await deployService.StopAsync(service, cancellationToken);
        if (stopResult.IsFailure)
        {
            service.MarkStopped();
            await dbContext.SaveChangesAsync(cancellationToken);
            return stopResult;
        }

        var startResult = await deployService.StartAsync(service, cancellationToken);
        if (startResult.IsFailure)
        {
            service.MarkStopped();
            await dbContext.SaveChangesAsync(cancellationToken);
            return startResult;
        }

        service.MarkDeployed();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}