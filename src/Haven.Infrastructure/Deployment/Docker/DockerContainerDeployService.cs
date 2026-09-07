using System.Net;

using Docker.DotNet;
using Docker.DotNet.Models;

using Haven.Application.Common;
using Haven.Application.Common.Contracts;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Configuration;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;
using Haven.Infrastructure.Utils;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Environment = Haven.Domain.Aggregates.Environment;

namespace Haven.Infrastructure.Deployment.Docker;

/// <summary>Deploys services sourced from a pre-built Docker image (<see cref="DockerConfig"/>).</summary>
public class DockerContainerDeployService : IDeployService
{
    private readonly ILogger<DockerContainerDeployService> _logger;
    private readonly IDockerClient _dockerClient;
    private readonly IDockerContainerRuntime _containerRuntime;
    private readonly INetworkRepository _networkRepository;
    private readonly INetworkingService _networkingService;
    private readonly IEnvironmentVariableService _environmentVariableService;
    private readonly IFeatureFlagService _featureFlagService;
    private readonly IDeploymentLogService _logService;
    private readonly IOptionsMonitor<VolumesOptions> _volumesOptions;
    private readonly IHostPathResolver _hostPathResolver;
    private readonly ITraefikLabelMerger _traefikLabelMerger;

    public DockerContainerDeployService(ILogger<DockerContainerDeployService> logger,
        IDockerClient dockerClient,
        IDockerContainerRuntime containerRuntime,
        INetworkRepository networkRepository,
        INetworkingServiceFactory networkingServiceFactory, IEnvironmentVariableService environmentVariableService,
        IFeatureFlagService featureFlagService, IDeploymentLogService logService,
        IOptionsMonitor<VolumesOptions> volumesOptions, IHostPathResolver hostPathResolver,
        ITraefikLabelMerger traefikLabelMerger)
    {
        _logger = logger;
        _dockerClient = dockerClient;
        _containerRuntime = containerRuntime;
        _networkRepository = networkRepository;
        _environmentVariableService = environmentVariableService;
        _featureFlagService = featureFlagService;
        _logService = logService;
        _volumesOptions = volumesOptions;
        _hostPathResolver = hostPathResolver;
        _traefikLabelMerger = traefikLabelMerger;
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

        var param = await BuildCreateContainerParametersAsync(service, dockerConfig, cancellationToken);

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

        await ConnectToEnvironmentNetworkAsync(service, cancellationToken);

        await _logService.AppendLogAsync(depId, "Container started successfully.", cancellationToken);

        _logger.LogInformation(
            "Successfully deployed service '{ServiceName}' from project '{ProjectName}' as a Docker Container",
            service.Name,
            project.Name);

        var inspect = await _dockerClient.Containers.InspectContainerAsync(createResult.Value, cancellationToken);
        return BuildDeployData(service, param.Name, inspect);
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

        var param = await BuildCreateContainerParametersAsync(service, dockerConfig, cancellationToken);

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

        await ConnectToEnvironmentNetworkAsync(service, cancellationToken);

        _logger.LogInformation(
            "Successfully started service '{ServiceName}' from project '{ProjectName}'",
            service.Name,
            project.Name);

        var inspect = await _dockerClient.Containers.InspectContainerAsync(createResult.Value, cancellationToken);
        return BuildDeployData(service, param.Name, inspect);
    }

    public async Task CleanupAsync(IDeployableContainer container, CancellationToken cancellationToken)
    {
        if (container is not Service service) return;
        await _containerRuntime.RemoveAllForOwnerAsync(service.Id, _networkingService, "cleaned up for deleted service", cancellationToken);
    }

    private async Task<CreateContainerParameters> BuildCreateContainerParametersAsync(Service service, DockerConfig dockerConfig, CancellationToken cancellationToken)
    {
        var envs = await _environmentVariableService.BuildVariablesForServiceAsync(service.Id, cancellationToken);
        var flags = await _featureFlagService.GetFlagsAsEnvironmentsForServiceAsync(service.Id, cancellationToken);
        envs.AddRange(flags);

        var volumesRootLocal = Path.GetFullPath(_volumesOptions.CurrentValue.RootPath);
        var volumesRootHost = await _hostPathResolver.ResolveAsync(volumesRootLocal, cancellationToken);
        var mounts = DockerUtils.BuildMounts(service, volumesRootLocal, volumesRootHost);

        await _containerRuntime.EnsureNamedVolumesReadyAsync(dockerConfig.Image, mounts, cancellationToken);

        _logger.LogDebug("Building container parameters for service '{ServiceName}': ExposureMode={ExposureMode}, PortCount={PortCount}, MountCount={MountCount}",
            service.Name, service.ExposureMode, dockerConfig.Ports.Count, mounts.Count);

        var name = DockerUtils.BuildContainerName(service.Environment?.Project?.Alias, service.Environment?.Alias, service.Alias, service.Name, service.Id);
        var labels = DockerUtils.BuildContainerLabels(service);
        await _traefikLabelMerger.MergeAsync(service, labels, cancellationToken);

        var param = _containerRuntime.BuildContainerParameters(name, labels, dockerConfig.Image, envs, service.ExposureMode, dockerConfig.Ports, mounts, dockerConfig.RestartPolicy, dockerConfig.CommandArgs);

        // A container created with no network specified lands on Docker's default "bridge" network
        // until it's explicitly connected to its Project/Environment network afterward. Discovery
        // tools that snapshot a container's networks right as it starts (e.g. Traefik's Docker
        // provider) can catch it mid-transition and lock onto the wrong (bridge) IP, since they don't
        // refresh on a later "network connect" event - so join the environment network from the
        // moment the container is created instead of connecting to it post-hoc.
        var environmentNetworkDockerId = await ResolveEnvironmentNetworkDockerIdAsync(service, cancellationToken);
        if (environmentNetworkDockerId is not null)
        {
            param.NetworkingConfig = new NetworkingConfig
            {
                EndpointsConfig = new Dictionary<string, EndpointSettings>
                {
                    { environmentNetworkDockerId, new EndpointSettings() }
                }
            };
        }

        return param;
    }

    private async Task<string?> ResolveEnvironmentNetworkDockerIdAsync(Service service, CancellationToken cancellationToken)
    {
        var environment = service.Environment;
        if (environment is null) return null;

        var networks = await _networkRepository.GetByProjectAndEnvironmentAsync(environment.ProjectId, environment.Id, cancellationToken);
        var network = networks.FirstOrDefault();
        if (network is null) return null;

        await _networkingService.EnsureNetworkExistsAsync(network.Id, cancellationToken);

        networks = await _networkRepository.GetByProjectAndEnvironmentAsync(environment.ProjectId, environment.Id, cancellationToken);
        return networks.FirstOrDefault()?.DockerNetworkId;
    }

    private async Task ConnectToEnvironmentNetworkAsync(Service service, CancellationToken cancellationToken)
    {
        // The Project/Environment network is already attached at container-creation time (see
        // ResolveEnvironmentNetworkDockerIdAsync); only Shared/External networks need connecting here.
        var networkIds = new List<Guid>();

        // Shared/external networks may already be assigned to this service (e.g. from creation time,
        // before any container existed) - connect the brand-new container to those too.
        var additionalNetworkIds = service.ServiceNetworks
            .Where(sn => sn.Network is not null && sn.Network.Type != NetworkType.ProjectEnvironment)
            .Select(sn => sn.NetworkId)
            .Distinct();
        networkIds.AddRange(additionalNetworkIds);

        if (networkIds.Count == 0) return;

        await _containerRuntime.ConnectToNetworksAsync(service.Id, networkIds, _networkingService, cancellationToken);
    }

    private static DeployData BuildDeployData(Service service, string containerName, ContainerInspectResponse inspect)
    {
        var rawIp = inspect.NetworkSettings.Networks.Values
            .Select(n => n.IPAddress)
            .FirstOrDefault(ip => !string.IsNullOrEmpty(ip));

        return new DeployData
        {
            ServiceId = service.Id,
            IpAddress = rawIp != null ? IPAddress.Parse(rawIp) : null,
            ContainerName = containerName,
            Ports = inspect.ExtractPortMappings()
        };
    }
}