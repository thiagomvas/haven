using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Services;
using Haven.Domain.Entities;
using Haven.Infrastructure.Persistence;

namespace Haven.Infrastructure.Deployment;

public class DeploymentOrchestrator(IUnitOfWork unitOfWork, IServiceRegistry registry, IDeployServiceFactory deployServiceFactory) : IDeploymentOrchestrator
{
    public async Task<Result> DeployServiceAsync(Service service, CancellationToken cancellationToken)
    {
        if (service is null) return Error.NotFound;
        if (service.Environment?.Project is null) return Error.NotFound;

        service.MarkDeploying();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var deployService = deployServiceFactory.Create(service);
        if (deployService is null)
            return Error.Failure("Deploy.NotSupported",
                "No deployment service available for the specified service type.");
        var deployResult = await deployService.DeployAsync(service, cancellationToken);

        if (deployResult.IsFailure)
            service.MarkStopped();
        else service.MarkDeployed();

        await unitOfWork.SaveChangesAsync(cancellationToken);
        
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