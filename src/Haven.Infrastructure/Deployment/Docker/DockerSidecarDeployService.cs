using System.Net;

using Docker.DotNet;
using Docker.DotNet.Models;

using Haven.Application.Common;
using Haven.Application.Common.Contracts;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;
using Haven.Infrastructure.Utils;

using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.Deployment.Docker;

/// <summary>
/// Deploys Haven-managed sidecar containers sourced from a pre-built Docker image
/// (<see cref="DockerConfig"/>). Mirrors <see cref="DockerContainerDeployService"/> but strips
/// everything that only makes sense for a Project/Environment-scoped <c>Service</c> (env vars,
/// feature flags, volumes, deployment-log persistence, the project/environment network) since
/// sidecars have none of those — they only join networks explicitly attached via
/// <see cref="Sidecar.SidecarNetworks"/>.
/// </summary>
public class DockerSidecarDeployService : IDeployService
{
    private readonly ILogger<DockerSidecarDeployService> _logger;
    private readonly IDockerClient _dockerClient;
    private readonly IDockerContainerRuntime _containerRuntime;
    private readonly INetworkRepository _networkRepository;
    private readonly INetworkingService _networkingService;

    public DockerSidecarDeployService(
        ILogger<DockerSidecarDeployService> logger,
        IDockerClient dockerClient,
        IDockerContainerRuntime containerRuntime,
        INetworkRepository networkRepository,
        INetworkingServiceFactory networkingServiceFactory)
    {
        _logger = logger;
        _dockerClient = dockerClient;
        _containerRuntime = containerRuntime;
        _networkRepository = networkRepository;
        _networkingService = networkingServiceFactory.Create(ServiceType.DockerImage)
            ?? throw new InvalidOperationException("No networking service found for DockerImage type");
    }

    public bool CanHandle(IDeployableContainer container) =>
        container is Sidecar sidecar && sidecar.SourceConfig is DockerConfig;

    public async Task<Result<DeployData>> DeployAsync(IDeployableContainer container, Guid? deploymentId, CancellationToken cancellationToken)
    {
        if (container is not Sidecar sidecar) return Error.NotSupported;

        var dockerConfig = sidecar.SourceConfig as DockerConfig;
        if (dockerConfig == null || string.IsNullOrWhiteSpace(dockerConfig.Image))
            return Error.InvalidSourceConfig;

        _logger.LogInformation("Pulling Docker image '{Image}' for sidecar '{SidecarName}'", dockerConfig.Image, sidecar.Name);

        try
        {
            await _dockerClient.Images.DeleteImageAsync(dockerConfig.Image, new ImageDeleteParameters { Force = true }, cancellationToken);
        }
        catch
        {
            _logger.LogDebug("Could not remove old image '{Image}', proceeding with pull", dockerConfig.Image);
        }

        try
        {
            await _dockerClient.Images.CreateImageAsync(new ImagesCreateParameters { FromImage = dockerConfig.Image }, null, new Progress<JSONMessage>(), cancellationToken);
        }
        catch (DockerApiException ex)
        {
            _logger.LogError(ex, "Failed to pull Docker image '{Image}' for sidecar '{SidecarName}'", dockerConfig.Image, sidecar.Name);
            return Error.Docker.InvalidImage;
        }

        // Only stop the old container now that the new image is fully pulled, to minimize downtime
        // and avoid marking the sidecar as stopped before the replacement is ready to start.
        await _containerRuntime.RemoveAllForOwnerAsync(sidecar.Id, _networkingService, "removed before redeploying", cancellationToken);

        _logger.LogInformation("Deploying sidecar '{SidecarName}' as a Docker Container", sidecar.Name);

        var param = BuildCreateContainerParameters(sidecar, dockerConfig);

        Result<string> createResult;
        try
        {
            createResult = await _containerRuntime.CreateAndStartAsync(param, cancellationToken);
        }
        catch (DockerApiException ex)
        {
            _logger.LogError(ex, "Failed to create/start Docker container for sidecar '{SidecarName}': {StatusCode} {Message}",
                sidecar.Name, ex.StatusCode, ex.Message);
            return Error.Docker.FailedToStartContainer;
        }

        if (createResult.IsFailure)
            return createResult.Error;

        await ConnectToAttachedNetworksAsync(sidecar, cancellationToken);

        _logger.LogInformation("Successfully deployed sidecar '{SidecarName}' as a Docker Container", sidecar.Name);

        var inspect = await _dockerClient.Containers.InspectContainerAsync(createResult.Value, cancellationToken);
        return BuildDeployData(sidecar, param.Name, inspect);
    }

    public async Task<Result> StopAsync(IDeployableContainer container, CancellationToken cancellationToken)
    {
        if (container is not Sidecar sidecar) return Error.NotSupported;

        var containers = await _containerRuntime.GetContainersByLabelAsync(DockerUtils.BuildIdLabel(sidecar.Id), cancellationToken);

        if (containers.Count == 0)
        {
            _logger.LogWarning("No Docker container found for sidecar '{SidecarName}' to stop", sidecar.Name);
            return Error.NotFoundFor("Docker Container", sidecar.Id);
        }

        await _containerRuntime.StopAndRemoveAsync((IReadOnlyCollection<ContainerListResponse>)containers, sidecar.Id, _networkingService,
            "stopped and removed", cancellationToken);

        return Result.Success();
    }

    public async Task<Result<DeployData>> StartAsync(IDeployableContainer container, CancellationToken cancellationToken)
    {
        if (container is not Sidecar sidecar) return Error.NotSupported;

        var dockerConfig = sidecar.SourceConfig as DockerConfig;
        if (dockerConfig == null || string.IsNullOrWhiteSpace(dockerConfig.Image))
            return Error.InvalidSourceConfig;

        _logger.LogInformation("Starting sidecar '{SidecarName}'", sidecar.Name);

        var param = BuildCreateContainerParameters(sidecar, dockerConfig);

        Result<string> createResult;
        try
        {
            createResult = await _containerRuntime.CreateAndStartAsync(param, cancellationToken);
        }
        catch (DockerApiException ex)
        {
            _logger.LogError(ex, "Failed to create/start Docker container for sidecar '{SidecarName}': {StatusCode} {Message}",
                sidecar.Name, ex.StatusCode, ex.Message);
            return Error.Docker.FailedToStartContainer;
        }

        if (createResult.IsFailure)
            return createResult.Error;

        await ConnectToAttachedNetworksAsync(sidecar, cancellationToken);

        _logger.LogInformation("Successfully started sidecar '{SidecarName}'", sidecar.Name);

        var inspect = await _dockerClient.Containers.InspectContainerAsync(createResult.Value, cancellationToken);
        return BuildDeployData(sidecar, param.Name, inspect);
    }

    public async Task CleanupAsync(IDeployableContainer container, CancellationToken cancellationToken)
    {
        if (container is not Sidecar sidecar) return;
        await _containerRuntime.RemoveAllForOwnerAsync(sidecar.Id, _networkingService, "cleaned up for deleted sidecar", cancellationToken);
    }

    private CreateContainerParameters BuildCreateContainerParameters(Sidecar sidecar, DockerConfig dockerConfig)
    {
        var name = DockerUtils.BuildSidecarContainerName(sidecar.Alias, sidecar.Name, sidecar.Id);
        var labels = DockerUtils.BuildSidecarContainerLabels(sidecar);

        return _containerRuntime.BuildContainerParameters(name, labels, dockerConfig.Image, envs: null,
            ExposureMode.Internal, dockerConfig.Ports, mounts: [], dockerConfig.RestartPolicy, dockerConfig.CommandArgs);
    }

    private async Task ConnectToAttachedNetworksAsync(Sidecar sidecar, CancellationToken cancellationToken)
    {
        var networkIds = sidecar.SidecarNetworks.Select(sn => sn.NetworkId).ToHashSet();

        var systemNetworks = await _networkRepository.GetAllAsync(NetworkType.System, cancellationToken);
        var systemNetwork = systemNetworks.FirstOrDefault();
        if (systemNetwork is not null)
            networkIds.Add(systemNetwork.Id);
        else
            _logger.LogWarning("No '{NetworkName}' network found; sidecar '{SidecarName}' will not auto-join the control plane network", DomainConstants.SystemNetworkName, sidecar.Name);

        if (networkIds.Count == 0) return;

        await _containerRuntime.ConnectToNetworksAsync(sidecar.Id, networkIds, _networkingService, cancellationToken);
    }

    private static DeployData BuildDeployData(Sidecar sidecar, string containerName, ContainerInspectResponse inspect)
    {
        var rawIp = inspect.NetworkSettings.Networks.Values
            .Select(n => n.IPAddress)
            .FirstOrDefault(ip => !string.IsNullOrEmpty(ip));

        return new DeployData
        {
            ServiceId = sidecar.Id,
            IpAddress = rawIp != null ? IPAddress.Parse(rawIp) : null,
            ContainerName = containerName,
            Ports = inspect.ExtractPortMappings()
        };
    }
}