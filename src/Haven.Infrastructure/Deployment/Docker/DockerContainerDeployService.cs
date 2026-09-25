using Docker.DotNet;
using Docker.DotNet.Models;

using Haven.Application.Common;
using Haven.Application.Common.Contracts;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;
using Haven.Infrastructure.Utils;

using Microsoft.Extensions.Logging;

using Environment = Haven.Domain.Aggregates.Environment;

namespace Haven.Infrastructure.Deployment.Docker;

/// <summary>Deploys services sourced from a pre-built Docker image (<see cref="DockerConfig"/>).</summary>
public class DockerContainerDeployService : IDeployService
{
    private readonly ILogger<DockerContainerDeployService> _logger;
    private readonly IDockerClient _dockerClient;
    private readonly IDockerContainerRuntime _containerRuntime;
    private readonly INetworkingService _networkingService;
    private readonly IDeploymentLogService _logService;

    public DockerContainerDeployService(ILogger<DockerContainerDeployService> logger,
        IDockerClient dockerClient,
        IDockerContainerRuntime containerRuntime,
        INetworkingServiceFactory networkingServiceFactory,
        IDeploymentLogService logService)
    {
        _logger = logger;
        _dockerClient = dockerClient;
        _containerRuntime = containerRuntime;
        _logService = logService;
        _networkingService = networkingServiceFactory.Create(ServiceType.DockerImage) ?? throw new InvalidOperationException("No networking service found for DockerImage type");
    }

    public bool CanHandle(IDeployableContainer container) =>
        container is Service { Type: ServiceType.DockerImage } service && service.SourceConfig is DockerConfig;

    public async Task<Result<DeployData>> DeployAsync(IDeployableContainer container, Guid? deploymentId, CancellationToken cancellationToken)
    {
        if (container is not Service service) return Error.NotSupported;
        if (deploymentId is not { } depId) return Error.Failed;

        var environment = service.Environment;
        if (environment == null) return Error.NotFoundFor(nameof(Environment), service.EnvironmentId);
        var project = environment.Project;
        if (project == null) return Error.NotFoundFor(nameof(Project), environment.ProjectId);

        var dockerConfig = service.SourceConfig as DockerConfig;
        if (dockerConfig == null || string.IsNullOrWhiteSpace(dockerConfig.Image))
            return Error.InvalidSourceConfig;

        _logger.LogInformation(
            "Pulling Docker image '{Image}' for service '{ServiceName}' from project '{ProjectName}'",
            dockerConfig.Image,
            service.Name,
            project.Name);

        await _logService.AppendLogAsync(depId, $"Pulling image '{dockerConfig.Image}'...", cancellationToken);

        try
        {
            await _dockerClient.Images.DeleteImageAsync(dockerConfig.Image, new ImageDeleteParameters { Force = true },
                cancellationToken);
        }
        catch
        {
            _logger.LogDebug("Could not remove old image '{Image}', proceeding with pull", dockerConfig.Image);
        }

        var pullProgress = new Progress<JSONMessage>(msg =>
        {
            if (!string.IsNullOrWhiteSpace(msg.Status))
                _ = _logService.AppendLogAsync(depId, msg.Status, cancellationToken);
        });

        try
        {
            await _dockerClient.Images.CreateImageAsync(new ImagesCreateParameters { FromImage = dockerConfig.Image },
                null,
                pullProgress,
                cancellationToken);
        }
        catch (DockerApiException ex)
        {
            _logger.LogError(ex, "Failed to pull Docker image '{Image}' for service '{ServiceName}'", dockerConfig.Image, service.Name);
            await _logService.AppendLogAsync(depId, $"Failed to pull image '{dockerConfig.Image}': {ex.Message}", cancellationToken);
            return Error.Docker.InvalidImage;
        }

        await _logService.AppendLogAsync(depId, $"Image '{dockerConfig.Image}' pulled successfully.", cancellationToken);

        // Only stop the old container now that the new image is fully pulled, to minimize downtime
        // and avoid marking the service as stopped before the replacement is ready to start.
        await _containerRuntime.RemoveAllForOwnerAsync(service.Id, _networkingService, "removed before redeploying", cancellationToken);

        _logger.LogInformation(
            "Deploying service '{ServiceName}' from project '{ProjectName}' as a Docker Container",
            service.Name,
            project.Name);

        var (param, environmentNetworkName) = await _containerRuntime.BuildServiceContainerParametersAsync(
            service, dockerConfig.Image, dockerConfig.Ports, dockerConfig.CommandArgs, dockerConfig.RestartPolicy,
            ensureNamedVolumesReady: true, _networkingService, cancellationToken);

        await _logService.AppendLogAsync(depId, "Creating and starting container...", cancellationToken);

        Result<string> createResult;
        try
        {
            createResult = await _containerRuntime.CreateAndStartAsync(param, cancellationToken);
        }
        catch (DockerApiException ex)
        {
            _logger.LogError(ex, "Failed to create/start Docker container for service '{ServiceName}': {StatusCode} {Message}",
                service.Name, ex.StatusCode, ex.Message);
            await _logService.AppendLogAsync(depId, $"Failed to create/start container: {ex.Message}", cancellationToken);
            return Error.Docker.FailedToStartContainer;
        }

        if (createResult.IsFailure)
        {
            await _logService.AppendLogAsync(depId, "Failed to start container.", cancellationToken);
            return createResult.Error;
        }

        await _containerRuntime.ConnectServiceToAssignedNetworksAsync(service, _networkingService, cancellationToken);

        await _logService.AppendLogAsync(depId, "Container started successfully.", cancellationToken);

        _logger.LogInformation(
            "Successfully deployed service '{ServiceName}' from project '{ProjectName}' as a Docker Container",
            service.Name,
            project.Name);

        var inspect = await _dockerClient.Containers.InspectContainerAsync(createResult.Value, cancellationToken);
        var deployData = _containerRuntime.BuildServiceDeployData(service, param.Name, inspect, environmentNetworkName);

        await _containerRuntime.HealTraefikRoutingBestEffortAsync(service.Id, deployData.IpAddress?.ToString(), cancellationToken);

        return deployData;
    }

    public async Task<Result> StopAsync(IDeployableContainer container, CancellationToken cancellationToken)
    {
        if (container is not Service service) return Error.NotSupported;

        var containers = await _containerRuntime.GetContainersByLabelAsync(DockerUtils.BuildIdLabel(service.Id), cancellationToken);

        if (containers.Count == 0)
        {
            _logger.LogWarning("No Docker container found for service '{ServiceName}' to stop", service.Name);
            return Error.NotFoundFor("Docker Container", service.Id);
        }

        await _containerRuntime.StopAndRemoveAsync((IReadOnlyCollection<ContainerListResponse>)containers, service.Id, _networkingService,
            "stopped and removed", cancellationToken);

        return Result.Success();
    }

    public async Task<Result<DeployData>> StartAsync(IDeployableContainer container, CancellationToken cancellationToken)
    {
        if (container is not Service service) return Error.NotSupported;

        var environment = service.Environment;
        if (environment == null) return Error.NotFoundFor(nameof(Environment), service.EnvironmentId);
        var project = environment.Project;
        if (project == null) return Error.NotFoundFor(nameof(Project), environment.ProjectId);

        var dockerConfig = service.SourceConfig as DockerConfig;
        if (dockerConfig == null || string.IsNullOrWhiteSpace(dockerConfig.Image))
            return Error.InvalidSourceConfig;

        _logger.LogInformation(
            "Starting service '{ServiceName}' from project '{ProjectName}'",
            service.Name,
            project.Name);

        var (param, environmentNetworkName) = await _containerRuntime.BuildServiceContainerParametersAsync(
            service, dockerConfig.Image, dockerConfig.Ports, dockerConfig.CommandArgs, dockerConfig.RestartPolicy,
            ensureNamedVolumesReady: true, _networkingService, cancellationToken);

        Result<string> createResult;
        try
        {
            createResult = await _containerRuntime.CreateAndStartAsync(param, cancellationToken);
        }
        catch (DockerApiException ex)
        {
            _logger.LogError(ex, "Failed to create/start Docker container for service '{ServiceName}': {StatusCode} {Message}",
                service.Name, ex.StatusCode, ex.Message);
            return Error.Docker.FailedToStartContainer;
        }

        if (createResult.IsFailure)
            return createResult.Error;

        await _containerRuntime.ConnectServiceToAssignedNetworksAsync(service, _networkingService, cancellationToken);

        _logger.LogInformation(
            "Successfully started service '{ServiceName}' from project '{ProjectName}'",
            service.Name,
            project.Name);

        var inspect = await _dockerClient.Containers.InspectContainerAsync(createResult.Value, cancellationToken);
        var deployData = _containerRuntime.BuildServiceDeployData(service, param.Name, inspect, environmentNetworkName);

        await _containerRuntime.HealTraefikRoutingBestEffortAsync(service.Id, deployData.IpAddress?.ToString(), cancellationToken);

        return deployData;
    }

    public async Task CleanupAsync(IDeployableContainer container, CancellationToken cancellationToken)
    {
        if (container is not Service service) return;
        await _containerRuntime.RemoveAllForOwnerAsync(service.Id, _networkingService, "cleaned up for deleted service", cancellationToken);
    }

}