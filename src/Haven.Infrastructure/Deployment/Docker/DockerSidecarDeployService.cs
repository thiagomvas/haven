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

        await _containerRuntime.RemoveAllForOwnerAsync(sidecar.Id, _networkingService, "removed before redeploying", cancellationToken);

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

        _logger.LogInformation("Deploying sidecar '{SidecarName}' as a Docker Container", sidecar.Name);

        var param = await BuildCreateContainerParametersAsync(sidecar, dockerConfig, cancellationToken);

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

        var param = await BuildCreateContainerParametersAsync(sidecar, dockerConfig, cancellationToken);

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

    private async Task<CreateContainerParameters> BuildCreateContainerParametersAsync(
        Sidecar sidecar, DockerConfig dockerConfig, CancellationToken cancellationToken)
    {
        var name = DockerUtils.BuildSidecarContainerName(sidecar.Alias, sidecar.Name, sidecar.Id);
        var labels = DockerUtils.BuildSidecarContainerLabels(sidecar);
        var mounts = BuildMounts(sidecar, dockerConfig);
        var exposureMode = BuildExposureMode(sidecar);

        var param = _containerRuntime.BuildContainerParameters(name, labels, dockerConfig.Image, envs: null,
            exposureMode, dockerConfig.Ports, mounts, dockerConfig.RestartPolicy, dockerConfig.CommandArgs);

        var systemNetworkDockerId = await ResolveSystemNetworkDockerIdAsync(cancellationToken);
        if (systemNetworkDockerId is not null)
        {
            param.NetworkingConfig = new NetworkingConfig
            {
                EndpointsConfig = new Dictionary<string, EndpointSettings>
                {
                    { systemNetworkDockerId, new EndpointSettings() }
                }
            };
        }

        return param;
    }

    /// <summary>
    /// A container created with no network specified lands on Docker's default "bridge" network,
    /// then gets reattached to Haven's networks afterward via <see cref="ConnectToAttachedNetworksAsync"/>.
    /// On hosts where the default bridge's port-publishing/NAT is broken, published ports never work
    /// even after that later reattachment - the container must join a real network from the moment
    /// it's created to avoid ever touching the default bridge.
    /// </summary>
    private async Task<string?> ResolveSystemNetworkDockerIdAsync(CancellationToken cancellationToken)
    {
        var systemNetworks = await _networkRepository.GetAllAsync(NetworkType.System, cancellationToken);
        var systemNetwork = systemNetworks.FirstOrDefault();
        if (systemNetwork is null) return null;

        await _networkingService.EnsureNetworkExistsAsync(systemNetwork.Id, cancellationToken);

        systemNetworks = await _networkRepository.GetAllAsync(NetworkType.System, cancellationToken);
        return systemNetworks.FirstOrDefault()?.DockerNetworkId;
    }

    /// <summary>
    /// Traefik is a reverse proxy meant to accept traffic from outside the host, so it binds its
    /// published ports to all interfaces (0.0.0.0) rather than Haven's usual loopback-only default
    /// for sidecars.
    /// </summary>
    private static ExposureMode BuildExposureMode(Sidecar sidecar) =>
        sidecar.Kind == SidecarKind.Traefik ? ExposureMode.External : ExposureMode.Internal;

    /// <summary>
    /// Traefik's Docker provider needs to talk to the Docker daemon to discover containers, so the
    /// socket is mounted automatically — this is an infrastructure requirement, not a
    /// user-configurable setting. Mounted read-write per Traefik's own documented setup
    /// (https://doc.traefik.io/traefik/setup/docker/); a read-only mount is not what upstream
    /// recommends or tests against.
    ///
    /// When the quick-setup SSL toggle (or a hand-rolled command arg) configures an ACME
    /// certificate resolver, a named volume is also auto-mounted at <c>/letsencrypt</c> so the
    /// issued certificates (<c>acme.json</c>) survive container restarts/redeploys.
    /// </summary>
    private static List<Mount> BuildMounts(Sidecar sidecar, DockerConfig dockerConfig)
    {
        if (sidecar.Kind != SidecarKind.Traefik)
            return [];

        var mounts = new List<Mount>
        {
            new Mount { Type = "bind", Source = "/var/run/docker.sock", Target = "/var/run/docker.sock" }
        };

        var acmeEnabled = dockerConfig.CommandArgs.Any(a =>
            a.StartsWith("--certificatesresolvers.", StringComparison.OrdinalIgnoreCase) && a.Contains(".acme."));
        if (acmeEnabled)
            mounts.Add(new Mount { Type = "volume", Source = "haven-traefik-acme", Target = "/letsencrypt" });

        return mounts;
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

        // Traefik routes to service containers by Docker DNS name, so it must be reachable on
        // every Project/Environment network - not just the ones explicitly attached to it.
        if (sidecar.Kind == SidecarKind.Traefik)
        {
            var environmentNetworks = await _networkRepository.GetAllAsync(NetworkType.ProjectEnvironment, cancellationToken);
            foreach (var network in environmentNetworks)
                networkIds.Add(network.Id);
        }

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