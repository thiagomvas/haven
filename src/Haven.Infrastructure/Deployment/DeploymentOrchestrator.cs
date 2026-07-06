using System.Diagnostics;

using Haven.Application.Common;
using Haven.Application.Common.Contracts;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Common.Telemetry;
using Haven.Domain;
using Haven.Domain.Entities;

namespace Haven.Infrastructure.Deployment;

public class DeploymentOrchestrator(
    IUnitOfWork unitOfWork,
    IServiceRegistry registry,
    IDeployServiceFactory deployServiceFactory,
    IDeploymentLogService logService,
    HavenMetrics metrics) : IDeploymentOrchestrator
{
    public async Task<Result> DeployServiceAsync(Service service, CancellationToken cancellationToken)
    {
        if (service is null) return Error.NotFound;
        if (service.Environment?.Project is null) return Error.NotFound;

        var tags = ServiceTags(service);

        service.MarkDeploying();
        var deployment = await logService.CreateDeploymentForServiceAsync(service.Id, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        metrics.DeploymentsStarted.Add(1, tags);

        var deployService = deployServiceFactory.Create(service);
        if (deployService is null)
            return Error.NotSupported;

        var sw = Stopwatch.StartNew();

        Result<DeployData> deployResult;
        try
        {
            deployResult = await deployService.DeployAsync(service, deployment.Id, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            service.MarkStopped();
            await logService.MarkDeploymentCancelledAsync(deployment.Id, CancellationToken.None);
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            metrics.DeploymentsCancelled.Add(1, tags);
            metrics.DeploymentDurationSeconds.Record(sw.Elapsed.TotalSeconds, WithResult(tags, "cancelled"));
            return Error.CancelledOperation;
        }

        sw.Stop();

        if (deployResult.IsFailure)
        {
            service.MarkStopped();
            await logService.MarkDeploymentFailedAsync(deployment.Id, CancellationToken.None);
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            metrics.DeploymentsFailed.Add(1, tags);
            metrics.DeploymentDurationSeconds.Record(sw.Elapsed.TotalSeconds, WithResult(tags, "failure"));
            return deployResult;
        }

        if (!await TryMarkDeployedAsync(service, cancellationToken))
        {
            await logService.MarkDeploymentFailedAsync(deployment.Id, CancellationToken.None);
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            metrics.DeploymentsFailed.Add(1, tags);
            metrics.DeploymentDurationSeconds.Record(sw.Elapsed.TotalSeconds, WithResult(tags, "failure"));
            return Error.Docker.ContainerCrashedAfterStart;
        }

        await logService.MarkDeploymentCompletedAsync(deployment.Id, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        metrics.DeploymentsSucceeded.Add(1, tags);
        metrics.DeploymentDurationSeconds.Record(sw.Elapsed.TotalSeconds, WithResult(tags, "success"));

        var entry = await registry.EnsureServiceRegisteredAsync(service.Id, cancellationToken);
        entry.UpdateRuntime(deployResult.Value.IpAddress?.ToString() ?? string.Empty, deployResult.Value.Ports ?? [], service.Status);
        entry.ContainerName = deployResult.Value.ContainerName;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> StopServiceAsync(Service service, CancellationToken cancellationToken)
    {
        var deployService = deployServiceFactory.Create(service);
        if (deployService is null)
            return Error.NotSupported;

        var tags = OperationTags(service, "stop");
        var sw = Stopwatch.StartNew();

        var stopResult = await deployService.StopAsync(service, cancellationToken);
        sw.Stop();

        if (stopResult.IsFailure)
        {
            metrics.ServiceOperations.Add(1, WithResult(tags, "failure"));
            metrics.ServiceOperationDurationSeconds.Record(sw.Elapsed.TotalSeconds, WithResult(tags, "failure"));
            return stopResult;
        }

        service.MarkStopped();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        metrics.ServiceOperations.Add(1, WithResult(tags, "success"));
        metrics.ServiceOperationDurationSeconds.Record(sw.Elapsed.TotalSeconds, WithResult(tags, "success"));
        return Result.Success();
    }

    public async Task<Result> StartServiceAsync(Service service, CancellationToken cancellationToken)
    {
        var deployService = deployServiceFactory.Create(service);
        if (deployService is null)
            return Error.NotSupported;

        service.MarkDeploying();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var tags = OperationTags(service, "start");
        var sw = Stopwatch.StartNew();

        var startResult = await deployService.StartAsync(service, cancellationToken);
        sw.Stop();

        if (startResult.IsFailure)
        {
            service.MarkStopped();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            metrics.ServiceOperations.Add(1, WithResult(tags, "failure"));
            metrics.ServiceOperationDurationSeconds.Record(sw.Elapsed.TotalSeconds, WithResult(tags, "failure"));
            return startResult.Error;
        }

        if (!await TryMarkDeployedAsync(service, cancellationToken))
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            metrics.ServiceOperations.Add(1, WithResult(tags, "failure"));
            metrics.ServiceOperationDurationSeconds.Record(sw.Elapsed.TotalSeconds, WithResult(tags, "failure"));
            return Error.Docker.ContainerCrashedAfterStart;
        }

        metrics.ServiceOperations.Add(1, WithResult(tags, "success"));
        metrics.ServiceOperationDurationSeconds.Record(sw.Elapsed.TotalSeconds, WithResult(tags, "success"));

        var entry = await registry.EnsureServiceRegisteredAsync(service.Id, cancellationToken);
        entry.UpdateRuntime(startResult.Value.IpAddress?.ToString() ?? string.Empty, startResult.Value.Ports ?? [], service.Status);
        entry.ContainerName = startResult.Value.ContainerName;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> RestartServiceAsync(Service service, CancellationToken cancellationToken)
    {
        var deployService = deployServiceFactory.Create(service);
        if (deployService is null)
            return Error.NotSupported;

        service.MarkDeploying();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var tags = OperationTags(service, "restart");
        var sw = Stopwatch.StartNew();

        var stopResult = await deployService.StopAsync(service, cancellationToken);
        if (stopResult.IsFailure)
        {
            sw.Stop();
            service.MarkStopped();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            metrics.ServiceOperations.Add(1, WithResult(tags, "failure"));
            metrics.ServiceOperationDurationSeconds.Record(sw.Elapsed.TotalSeconds, WithResult(tags, "failure"));
            return stopResult;
        }

        var startResult = await deployService.StartAsync(service, cancellationToken);
        sw.Stop();

        if (startResult.IsFailure)
        {
            service.MarkStopped();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            metrics.ServiceOperations.Add(1, WithResult(tags, "failure"));
            metrics.ServiceOperationDurationSeconds.Record(sw.Elapsed.TotalSeconds, WithResult(tags, "failure"));
            return startResult;
        }

        if (!await TryMarkDeployedAsync(service, cancellationToken))
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            metrics.ServiceOperations.Add(1, WithResult(tags, "failure"));
            metrics.ServiceOperationDurationSeconds.Record(sw.Elapsed.TotalSeconds, WithResult(tags, "failure"));
            return Error.Docker.ContainerCrashedAfterStart;
        }

        metrics.ServiceOperations.Add(1, WithResult(tags, "success"));
        metrics.ServiceOperationDurationSeconds.Record(sw.Elapsed.TotalSeconds, WithResult(tags, "success"));

        var entry = await registry.EnsureServiceRegisteredAsync(service.Id, cancellationToken);
        entry.UpdateRuntime(startResult.Value.IpAddress?.ToString() ?? string.Empty, startResult.Value.Ports ?? [], service.Status);
        entry.ContainerName = startResult.Value.ContainerName;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    /// <summary>
    /// Marks the service as deployed unless a reactive Docker event (e.g. the container dying
    /// immediately after start) already recorded it as Stopped/Degraded while this deployment
    /// was in flight. Reloading picks up that concurrent write so it isn't clobbered back to Running.
    /// </summary>
    private async Task<bool> TryMarkDeployedAsync(Service service, CancellationToken cancellationToken)
    {
        await unitOfWork.ReloadAsync(service, cancellationToken);

        if (service.Status is ServiceStatus.Stopped or ServiceStatus.Degraded)
            return false;

        service.MarkDeployed();
        return true;
    }

    private static TagList ServiceTags(Service service) => new()
    {
        { HavenMetrics.TagService, service.Name },
        { HavenMetrics.TagEnvironment, service.Environment?.Name ?? "unknown" },
        { HavenMetrics.TagProject, service.Environment?.Project?.Name ?? "unknown" },
        { HavenMetrics.TagServiceType, service.Type.ToString() },
    };

    private static TagList OperationTags(Service service, string operation)
    {
        var tags = ServiceTags(service);
        tags.Add(HavenMetrics.TagOperation, operation);
        return tags;
    }

    private static TagList WithResult(TagList tags, string result)
    {
        tags.Add(HavenMetrics.TagResult, result);
        return tags;
    }
}