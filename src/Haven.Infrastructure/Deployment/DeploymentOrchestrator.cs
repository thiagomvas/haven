using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Services;
using Haven.Domain.Entities;

namespace Haven.Infrastructure.Deployment;

public class DeploymentOrchestrator(IUnitOfWork unitOfWork, IServiceRegistry registry, IDeployServiceFactory deployServiceFactory, IDeploymentLogService logService) : IDeploymentOrchestrator
{
    public async Task<Result> DeployServiceAsync(Service service, CancellationToken cancellationToken)
    {
        if (service is null) return Error.NotFound;
        if (service.Environment?.Project is null) return Error.NotFound;

        service.MarkDeploying();
        var deployment = await logService.CreateDeploymentForServiceAsync(service.Id, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var deployService = deployServiceFactory.Create(service);
        if (deployService is null)
            return Error.Failure("Deploy.NotSupported",
                "No deployment service available for the specified service type.");

        Result deployResult;
        try
        {
            deployResult = await deployService.DeployAsync(service, deployment.Id, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            service.MarkStopped();
            await logService.MarkDeploymentCancelledAsync(deployment.Id, CancellationToken.None);
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            return Error.Failure("Deploy.Cancelled", "Deployment was cancelled.");
        }

        if (deployResult.IsFailure)
        {
            service.MarkStopped();
            await logService.MarkDeploymentFailedAsync(deployment.Id, CancellationToken.None);
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            return deployResult;
        }

        await logService.MarkDeploymentCompletedAsync(deployment.Id, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        var entry = await registry.EnsureServiceRegisteredAsync(service.Id, cancellationToken);
        entry.UpdateRuntime(deployResult.Value.IpAddress?.ToString() ?? string.Empty, deployResult.Value.Port ?? 0, service.Status);
        entry.ContainerName = deployResult.Value.ContainerName;
        await unitOfWork.SaveChangesAsync(cancellationToken);

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
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> StartServiceAsync(Service service, CancellationToken cancellationToken)
    {
        var deployService = deployServiceFactory.Create(service);
        if (deployService is null)
            return Error.Failure("Deploy.NotSupported",
                "No deployment service available for the specified service type.");

        service.MarkDeploying();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var startResult = await deployService.StartAsync(service, cancellationToken);
        if (startResult.IsFailure)
        {
            service.MarkStopped();
        }
        else
        {
            service.MarkDeployed();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var entry = await registry.EnsureServiceRegisteredAsync(service.Id, cancellationToken);
        entry.UpdateRuntime(startResult.Value.IpAddress?.ToString() ?? string.Empty, startResult.Value.Port ?? 0, service.Status);
        entry.ContainerName = startResult.Value.ContainerName;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> RestartServiceAsync(Service service, CancellationToken cancellationToken)
    {
        var deployService = deployServiceFactory.Create(service);
        if (deployService is null)
            return Error.Failure("Deploy.NotSupported",
                "No deployment service available for the specified service type.");

        service.MarkDeploying();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var stopResult = await deployService.StopAsync(service, cancellationToken);
        if (stopResult.IsFailure)
        {
            service.MarkStopped();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return stopResult;
        }

        var startResult = await deployService.StartAsync(service, cancellationToken);
        if (startResult.IsFailure)
        {
            service.MarkStopped();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return startResult;
        }

        service.MarkDeployed();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
