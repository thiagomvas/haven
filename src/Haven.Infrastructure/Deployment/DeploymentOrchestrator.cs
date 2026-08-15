using System.Diagnostics;

using Haven.Application.Common;
using Haven.Application.Common.Contracts;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Common.Telemetry;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;

using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.Deployment;

public class DeploymentOrchestrator(
    IUnitOfWork unitOfWork,
    IServiceRegistry registry,
    IDeployServiceFactory deployServiceFactory,
    IDeploymentLogService logService,
    HavenMetrics metrics,
    ILogger<DeploymentOrchestrator> logger) : IDeploymentOrchestrator
{
    public async Task<Result> DeployAsync(IDeployableContainer container, CancellationToken cancellationToken)
    {
        if (container is null) return Error.NotFound;
        if (container is Service serviceContainer && serviceContainer.Environment?.Project is null) return Error.NotFound;

        var deployService = deployServiceFactory.Create(container);
        if (deployService is null)
            return Error.NotSupported;

        var tags = Tags(container);

        container.MarkDeploying();

        // Deployment-log persistence only exists for Service today (Deployment.ServiceId is a
        // required FK); sidecars aren't logged there yet, so deploymentId stays null for them.
        Domain.Entities.Deployment? deployment = container is Service loggedService
            ? await logService.CreateDeploymentForServiceAsync(loggedService.Id, cancellationToken)
            : null;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        metrics.DeploymentsStarted.Add(1, tags);

        var sw = Stopwatch.StartNew();

        Result<DeployData> deployResult;
        try
        {
            deployResult = await deployService.DeployAsync(container, deployment?.Id, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            container.MarkStopped();
            if (deployment is not null) await logService.MarkDeploymentCancelledAsync(deployment.Id, CancellationToken.None);
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            metrics.DeploymentsCancelled.Add(1, tags);
            metrics.DeploymentDurationSeconds.Record(sw.Elapsed.TotalSeconds, WithResult(tags, "cancelled"));
            return Error.CancelledOperation;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "Unhandled exception while deploying '{Name}' ({Id})", container.Name, container.Id);
            container.MarkStopped();
            if (deployment is not null) await logService.MarkDeploymentFailedAsync(deployment.Id, CancellationToken.None);
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            metrics.DeploymentsFailed.Add(1, tags);
            metrics.DeploymentDurationSeconds.Record(sw.Elapsed.TotalSeconds, WithResult(tags, "failure"));
            return Error.Failed;
        }

        sw.Stop();

        if (deployResult.IsFailure)
        {
            container.MarkStopped();
            if (deployment is not null) await logService.MarkDeploymentFailedAsync(deployment.Id, CancellationToken.None);
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            metrics.DeploymentsFailed.Add(1, tags);
            metrics.DeploymentDurationSeconds.Record(sw.Elapsed.TotalSeconds, WithResult(tags, "failure"));
            return deployResult;
        }

        if (!await TryMarkDeployedAsync(container, cancellationToken))
        {
            if (deployment is not null) await logService.MarkDeploymentFailedAsync(deployment.Id, CancellationToken.None);
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            metrics.DeploymentsFailed.Add(1, tags);
            metrics.DeploymentDurationSeconds.Record(sw.Elapsed.TotalSeconds, WithResult(tags, "failure"));
            return Error.Docker.ContainerCrashedAfterStart;
        }

        if (deployment is not null) await logService.MarkDeploymentCompletedAsync(deployment.Id, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        metrics.DeploymentsSucceeded.Add(1, tags);
        metrics.DeploymentDurationSeconds.Record(sw.Elapsed.TotalSeconds, WithResult(tags, "success"));

        // Service registry entries only exist for Service today.
        if (container is Service registeredService)
        {
            var entry = await registry.EnsureServiceRegisteredAsync(registeredService.Id, cancellationToken);
            entry.UpdateRuntime(deployResult.Value.IpAddress?.ToString() ?? string.Empty, deployResult.Value.Ports ?? [], registeredService.Status);
            entry.ContainerName = deployResult.Value.ContainerName;
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }

    public async Task<Result> StopAsync(IDeployableContainer container, CancellationToken cancellationToken)
    {
        var deployService = deployServiceFactory.Create(container);
        if (deployService is null)
            return Error.NotSupported;

        var tags = OperationTags(container, "stop");
        var sw = Stopwatch.StartNew();

        var stopResult = await deployService.StopAsync(container, cancellationToken);
        sw.Stop();

        if (stopResult.IsFailure)
        {
            metrics.ServiceOperations.Add(1, WithResult(tags, "failure"));
            metrics.ServiceOperationDurationSeconds.Record(sw.Elapsed.TotalSeconds, WithResult(tags, "failure"));
            return stopResult;
        }

        // Reload guards against a concurrent Docker-event write clobbering the status.
        await unitOfWork.ReloadAsync(container, cancellationToken);

        container.MarkStopped();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        metrics.ServiceOperations.Add(1, WithResult(tags, "success"));
        metrics.ServiceOperationDurationSeconds.Record(sw.Elapsed.TotalSeconds, WithResult(tags, "success"));
        return Result.Success();
    }

    public async Task<Result> StartAsync(IDeployableContainer container, CancellationToken cancellationToken)
    {
        var deployService = deployServiceFactory.Create(container);
        if (deployService is null)
            return Error.NotSupported;

        container.MarkDeploying();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var tags = OperationTags(container, "start");
        var sw = Stopwatch.StartNew();

        var startResult = await deployService.StartAsync(container, cancellationToken);
        sw.Stop();

        if (startResult.IsFailure)
        {
            container.MarkStopped();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            metrics.ServiceOperations.Add(1, WithResult(tags, "failure"));
            metrics.ServiceOperationDurationSeconds.Record(sw.Elapsed.TotalSeconds, WithResult(tags, "failure"));
            return startResult.Error;
        }

        if (!await TryMarkDeployedAsync(container, cancellationToken))
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            metrics.ServiceOperations.Add(1, WithResult(tags, "failure"));
            metrics.ServiceOperationDurationSeconds.Record(sw.Elapsed.TotalSeconds, WithResult(tags, "failure"));
            return Error.Docker.ContainerCrashedAfterStart;
        }

        metrics.ServiceOperations.Add(1, WithResult(tags, "success"));
        metrics.ServiceOperationDurationSeconds.Record(sw.Elapsed.TotalSeconds, WithResult(tags, "success"));

        if (container is Service registeredService)
        {
            var entry = await registry.EnsureServiceRegisteredAsync(registeredService.Id, cancellationToken);
            entry.UpdateRuntime(startResult.Value.IpAddress?.ToString() ?? string.Empty, startResult.Value.Ports ?? [], registeredService.Status);
            entry.ContainerName = startResult.Value.ContainerName;
        }
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> RestartAsync(IDeployableContainer container, CancellationToken cancellationToken)
    {
        var deployService = deployServiceFactory.Create(container);
        if (deployService is null)
            return Error.NotSupported;

        container.MarkDeploying();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var tags = OperationTags(container, "restart");
        var sw = Stopwatch.StartNew();

        var stopResult = await deployService.StopAsync(container, cancellationToken);
        if (stopResult.IsFailure)
        {
            sw.Stop();
            container.MarkStopped();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            metrics.ServiceOperations.Add(1, WithResult(tags, "failure"));
            metrics.ServiceOperationDurationSeconds.Record(sw.Elapsed.TotalSeconds, WithResult(tags, "failure"));
            return stopResult;
        }

        var startResult = await deployService.StartAsync(container, cancellationToken);
        sw.Stop();

        if (startResult.IsFailure)
        {
            container.MarkStopped();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            metrics.ServiceOperations.Add(1, WithResult(tags, "failure"));
            metrics.ServiceOperationDurationSeconds.Record(sw.Elapsed.TotalSeconds, WithResult(tags, "failure"));
            return startResult;
        }

        if (!await TryMarkDeployedAsync(container, cancellationToken))
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            metrics.ServiceOperations.Add(1, WithResult(tags, "failure"));
            metrics.ServiceOperationDurationSeconds.Record(sw.Elapsed.TotalSeconds, WithResult(tags, "failure"));
            return Error.Docker.ContainerCrashedAfterStart;
        }

        metrics.ServiceOperations.Add(1, WithResult(tags, "success"));
        metrics.ServiceOperationDurationSeconds.Record(sw.Elapsed.TotalSeconds, WithResult(tags, "success"));

        if (container is Service registeredService)
        {
            var entry = await registry.EnsureServiceRegisteredAsync(registeredService.Id, cancellationToken);
            entry.UpdateRuntime(startResult.Value.IpAddress?.ToString() ?? string.Empty, startResult.Value.Ports ?? [], registeredService.Status);
            entry.ContainerName = startResult.Value.ContainerName;
        }
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    /// <summary>
    /// Marks the container as deployed unless a reactive Docker event (e.g. the container dying
    /// immediately after start) already recorded it as Stopped/Degraded while this deployment
    /// was in flight. Reloading picks up that concurrent write so it isn't clobbered back to Running.
    /// </summary>
    private async Task<bool> TryMarkDeployedAsync(IDeployableContainer container, CancellationToken cancellationToken)
    {
        await unitOfWork.ReloadAsync(container, cancellationToken);

        if (container.Status is ServiceStatus.Stopped)
            return false;

        container.MarkDeployed();
        return true;
    }

    private static TagList Tags(IDeployableContainer container) => container switch
    {
        Service service => new TagList
        {
            { HavenMetrics.TagService, service.Name },
            { HavenMetrics.TagEnvironment, service.Environment?.Name ?? "unknown" },
            { HavenMetrics.TagProject, service.Environment?.Project?.Name ?? "unknown" },
            { HavenMetrics.TagServiceType, service.Type.ToString() },
        },
        Sidecar sidecar => new TagList
        {
            { HavenMetrics.TagSidecar, sidecar.Name },
            { HavenMetrics.TagSidecarKind, sidecar.Kind.ToString() },
        },
        _ => new TagList()
    };

    private static TagList OperationTags(IDeployableContainer container, string operation)
    {
        var tags = Tags(container);
        tags.Add(HavenMetrics.TagOperation, operation);
        return tags;
    }

    private static TagList WithResult(TagList tags, string result)
    {
        tags.Add(HavenMetrics.TagResult, result);
        return tags;
    }
}
