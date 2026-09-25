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
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Infrastructure.Utils;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using RestartPolicy = Docker.DotNet.Models.RestartPolicy;

namespace Haven.Infrastructure.Deployment.Docker;

/// <inheritdoc cref="IDockerContainerRuntime" />
public sealed class DockerContainerRuntime : IDockerContainerRuntime
{
    private readonly IDockerClient _dockerClient;
    private readonly ILogger<DockerContainerRuntime> _logger;
    private readonly INetworkRepository _networkRepository;
    private readonly IOptionsMonitor<VolumesOptions> _volumesOptions;
    private readonly IHostPathResolver _hostPathResolver;
    private readonly ITraefikLabelMerger _traefikLabelMerger;
    private readonly ITraefikRoutingHealer _traefikRoutingHealer;
    private readonly IContainerEnvironmentService _containerEnvironmentService;
    private readonly IDockerContainerInspector _containerInspector;

    public DockerContainerRuntime(
        IDockerClient dockerClient,
        ILogger<DockerContainerRuntime> logger,
        INetworkRepository networkRepository,
        IOptionsMonitor<VolumesOptions> volumesOptions,
        IHostPathResolver hostPathResolver,
        ITraefikLabelMerger traefikLabelMerger,
        ITraefikRoutingHealer traefikRoutingHealer, IContainerEnvironmentService containerEnvironmentService,
        IDockerContainerInspector containerInspector)
    {
        _dockerClient = dockerClient;
        _logger = logger;
        _networkRepository = networkRepository;
        _volumesOptions = volumesOptions;
        _hostPathResolver = hostPathResolver;
        _traefikLabelMerger = traefikLabelMerger;
        _traefikRoutingHealer = traefikRoutingHealer;
        _containerEnvironmentService = containerEnvironmentService;
        _containerInspector = containerInspector;
    }

    public CreateContainerParameters BuildContainerParameters(
        string name,
        IDictionary<string, string> labels,
        string image,
        IEnumerable<EnvironmentVariables>? envs,
        ExposureMode exposureMode,
        IReadOnlyList<string> ports,
        IList<Mount> mounts,
        Haven.Domain.Enums.RestartPolicy restartPolicy,
        IReadOnlyList<string> commandArgs)
    {
        var envVars = DockerUtils.BuildEnvironmentVariableStrings(envs);
        var hostConfig = new HostConfig();
        var param = new CreateContainerParameters { Name = name, Labels = labels, Image = image, };

        var listenAddress = DockerUtils.TryBuildListenAddress(exposureMode);
        if (listenAddress != null)
        {
            envVars.Add($"LISTEN_ADDRESS={listenAddress}");

            if (ports.Count > 0)
            {
                var bindings = DockerUtils.BuildPortBindings(ports, exposureMode, listenAddress);
                foreach (var warning in bindings.Warnings)
                    _logger.LogWarning("{Warning}", warning);

                param.ExposedPorts = bindings.ExposedPorts;
                hostConfig.PortBindings = bindings.PortBindings;
            }
        }

        if (mounts.Count > 0)
            hostConfig.Mounts = mounts;

        hostConfig.RestartPolicy = MapRestartPolicy(restartPolicy);
        param.HostConfig = hostConfig;

        if (envVars.Count > 0)
            param.Env = envVars;

        if (commandArgs.Count > 0)
            param.Cmd = commandArgs.ToList();

        return param;
    }

    public async Task<Result<string>> CreateAndStartAsync(CreateContainerParameters parameters,
        CancellationToken cancellationToken)
    {
        var response = await _dockerClient.Containers.CreateContainerAsync(parameters, cancellationToken);

        var started =
            await _dockerClient.Containers.StartContainerAsync(response.ID, new ContainerStartParameters(),
                cancellationToken);

        if (!started)
        {
            _logger.LogError("Failed to start Docker container '{ContainerId}'", response.ID);
            return Error.Docker.FailedToStartContainer;
        }

        return response.ID;
    }

    public async Task EnsureNamedVolumesReadyAsync(string image, IEnumerable<Mount> mounts,
        CancellationToken cancellationToken)
    {
        foreach (var mount in mounts)
        {
            if (mount.Type != "volume" || string.IsNullOrEmpty(mount.Source))
                continue;

            var exists = true;
            try
            {
                await _dockerClient.Volumes.InspectAsync(mount.Source, cancellationToken);
            }
            catch (DockerApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                exists = false;
            }

            if (exists)
                continue;

            await _dockerClient.Volumes.CreateAsync(new VolumesCreateParameters { Name = mount.Source },
                cancellationToken);

            string? user;
            try
            {
                var imageInspect = await _dockerClient.Images.InspectImageAsync(image, cancellationToken);
                user = imageInspect.Config?.User;
            }
            catch (DockerApiException ex)
            {
                _logger.LogWarning(ex, "Could not inspect image '{Image}' to determine ownership for volume '{Volume}'",
                    image, mount.Source);
                continue;
            }

            if (string.IsNullOrWhiteSpace(user) || user is "root" or "0" or "0:0")
                continue;

            await ChownVolumeAsync(image, mount.Source, mount.Target, user, cancellationToken);
        }
    }

    /// <summary>
    /// Runs a short-lived, auto-removed helper container from <paramref name="image"/> — forced to
    /// run as root regardless of the image's own <c>USER</c> — that chowns <paramref name="volumeName"/>'s
    /// mountpoint to <paramref name="user"/>. Using the same image (rather than a generic busybox
    /// helper) means <paramref name="user"/> resolves correctly even when it's a name (e.g. "node")
    /// rather than a numeric uid, since only that image's own /etc/passwd has the mapping.
    /// </summary>
    private async Task ChownVolumeAsync(string image, string volumeName, string target, string user,
        CancellationToken cancellationToken)
    {
        var helperParams = new CreateContainerParameters
        {
            Image = image,
            User = "0:0",
            Entrypoint = new List<string> { "chown" },
            Cmd = new List<string> { "-R", user, target },
            HostConfig = new HostConfig
            {
                Mounts =
                    new List<Mount> { new Mount { Type = "volume", Source = volumeName, Target = target } },
                AutoRemove = true
            }
        };

        try
        {
            var created = await _dockerClient.Containers.CreateContainerAsync(helperParams, cancellationToken);
            await _dockerClient.Containers.StartContainerAsync(created.ID, new ContainerStartParameters(),
                cancellationToken);
            await _dockerClient.Containers.WaitContainerAsync(created.ID, cancellationToken);
            _logger.LogInformation("Fixed ownership of named volume '{Volume}' to '{User}' for image '{Image}'",
                volumeName, user, image);
        }
        catch (DockerApiException ex)
        {
            _logger.LogWarning(ex,
                "Failed to fix ownership of named volume '{Volume}' for image '{Image}'; container may fail to start if it requires non-root write access",
                volumeName, image);
        }
    }

    public async Task ConnectToNetworksAsync(Guid ownerId, IReadOnlyCollection<Guid> networkIds,
        INetworkingService networkingService, CancellationToken cancellationToken)
    {
        if (networkIds.Count == 0)
            return;

        var result = await networkingService.ConnectServiceToNetworksAsync(ownerId, networkIds, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning(
                "Failed to connect '{OwnerId}' to networks, but container is running",
                ownerId);
        }
    }

    public async Task<Result> ConnectContainerToNetworkAsync(string containerId, string dockerNetworkId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _dockerClient.Networks.ConnectNetworkAsync(
                dockerNetworkId,
                new NetworkConnectParameters { Container = containerId, EndpointConfig = new EndpointSettings() },
                cancellationToken);

            return Result.Success();
        }
        catch (DockerApiException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            return Result.Success();
        }
        catch (DockerApiException ex)
        {
            _logger.LogWarning(ex, "Failed to connect container '{ContainerId}' to network '{NetworkId}'", containerId,
                dockerNetworkId);
            return Error.Docker.OperationFailed(ex.Message);
        }
    }

    public Task<IList<ContainerListResponse>> GetContainersByLabelAsync(KeyValuePair<string, string> label,
        CancellationToken cancellationToken)
        => _containerInspector.GetContainersByLabelAsync(label, cancellationToken);

    public async Task StopAndRemoveAsync(IReadOnlyCollection<ContainerListResponse> containers, Guid ownerId,
        INetworkingService networkingService, string reason, CancellationToken cancellationToken)
    {
        await networkingService.DisconnectServiceFromAllNetworksAsync(ownerId, cancellationToken);

        foreach (var container in containers)
        {
            if (container.State == "running")
            {
                try
                {
                    await _dockerClient.Containers.StopContainerAsync(container.ID, new ContainerStopParameters(),
                        cancellationToken);
                }
                catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
                {
                    _logger.LogDebug("Timeout stopping container '{ContainerId}', proceeding with removal",
                        container.ID);
                }
            }

            await _dockerClient.Containers.RemoveContainerAsync(container.ID,
                new ContainerRemoveParameters { Force = true }, cancellationToken);
            _logger.LogInformation("Docker container '{ContainerId}' {Reason} (owner '{OwnerId}')", container.ID,
                reason, ownerId);
        }
    }

    public async Task RemoveAllForOwnerAsync(Guid ownerId, INetworkingService networkingService, string reason,
        CancellationToken cancellationToken)
    {
        var containers = await GetContainersByLabelAsync(DockerUtils.BuildIdLabel(ownerId), cancellationToken);

        if (containers.Count > 0)
            await StopAndRemoveAsync((IReadOnlyCollection<ContainerListResponse>)containers, ownerId, networkingService,
                reason, cancellationToken);
    }

    public Task<Result<ContainerInspectResponse>> InspectByServiceIdAsync(Guid serviceId,
        CancellationToken cancellationToken)
        => _containerInspector.InspectByServiceIdAsync(serviceId, cancellationToken);

    public Task<Result> RestartByServiceIdAsync(Guid ownerId, CancellationToken cancellationToken)
        => _containerInspector.RestartByServiceIdAsync(ownerId, cancellationToken);

    public async Task<Result<(long ExitCode, string StdOut, string StdErr)>> ExecInContainerByServiceIdAsync(
        Guid serviceId, string command, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var containers = await GetContainersByLabelAsync(DockerUtils.BuildIdLabel(serviceId), cancellationToken);

        var container = containers.FirstOrDefault();
        if (container is null)
        {
            _logger.LogWarning("No Docker container found for service '{ServiceId}'", serviceId);
            return Error.Docker.ContainerNotFound;
        }

        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        var execCreateResponse = await _dockerClient.Exec.ExecCreateContainerAsync(
            container.ID,
            new ContainerExecCreateParameters
            {
                AttachStdout = true,
                AttachStderr = true,
                Cmd = ["/bin/sh", "-c", command]
            },
            linkedCts.Token);

        using var stream =
            await _dockerClient.Exec.StartAndAttachContainerExecAsync(execCreateResponse.ID, false, linkedCts.Token);
        var (stdout, stderr) = await stream.ReadOutputToEndAsync(linkedCts.Token);

        var inspectResponse =
            await _dockerClient.Exec.InspectContainerExecAsync(execCreateResponse.ID, cancellationToken);

        return (inspectResponse.ExitCode, stdout, stderr);
    }

    public async Task<(CreateContainerParameters Param, string? EnvironmentNetworkName)>
        BuildServiceContainerParametersAsync(
            Service service,
            string image,
            IReadOnlyList<string> ports,
            IReadOnlyList<string> commandArgs,
            Haven.Domain.Enums.RestartPolicy restartPolicy,
            bool ensureNamedVolumesReady,
            INetworkingService networkingService,
            CancellationToken cancellationToken)
    {
        var envs = await _containerEnvironmentService.BuildEnvironmentVariablesAsync(service.Id, cancellationToken);

        var volumesRootLocal = Path.GetFullPath(_volumesOptions.CurrentValue.RootPath);
        var volumesRootHost = await _hostPathResolver.ResolveAsync(volumesRootLocal, cancellationToken);
        var mounts = DockerUtils.BuildMounts(service, volumesRootLocal, volumesRootHost);

        if (ensureNamedVolumesReady)
            await EnsureNamedVolumesReadyAsync(image, mounts, cancellationToken);

        _logger.LogDebug(
            "Building container parameters for service '{ServiceName}': ExposureMode={ExposureMode}, PortCount={PortCount}, MountCount={MountCount}",
            service.Name, service.ExposureMode, ports.Count, mounts.Count);

        var name = DockerUtils.BuildContainerName(service.Environment?.Project?.Alias, service.Environment?.Alias,
            service.Alias, service.Name, service.Id);
        var labels = DockerUtils.BuildContainerLabels(service);
        await _traefikLabelMerger.MergeAsync(service, name, labels, cancellationToken);

        var param = BuildContainerParameters(name, labels, image, envs, service.ExposureMode, ports, mounts,
            restartPolicy, commandArgs);

        var (environmentNetworkDockerId, environmentNetworkName) =
            await ResolveEnvironmentNetworkDockerIdAsync(service, networkingService, cancellationToken);
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

        return (param, environmentNetworkName);
    }

    private async Task<(string? DockerNetworkId, string? Name)> ResolveEnvironmentNetworkDockerIdAsync(Service service,
        INetworkingService networkingService, CancellationToken cancellationToken)
    {
        var environment = service.Environment;
        if (environment is null) return (null, null);

        var networks =
            await _networkRepository.GetByProjectAndEnvironmentAsync(environment.ProjectId, environment.Id,
                cancellationToken);
        var network = networks.FirstOrDefault();
        if (network is null) return (null, null);

        await networkingService.EnsureNetworkExistsAsync(network.Id, cancellationToken);

        networks = await _networkRepository.GetByProjectAndEnvironmentAsync(environment.ProjectId, environment.Id,
            cancellationToken);
        network = networks.FirstOrDefault();
        return (network?.DockerNetworkId, network?.Name);
    }

    public async Task ConnectServiceToAssignedNetworksAsync(Service service, INetworkingService networkingService,
        CancellationToken cancellationToken)
    {
        var networkIds = new List<Guid>();

        var additionalNetworkIds = service.ServiceNetworks
            .Where(sn => sn.Network is not null && sn.Network.Type != NetworkType.ProjectEnvironment)
            .Select(sn => sn.NetworkId)
            .Distinct();
        networkIds.AddRange(additionalNetworkIds);

        if (networkIds.Count == 0) return;

        await ConnectToNetworksAsync(service.Id, networkIds, networkingService, cancellationToken);
    }

    public DeployData BuildServiceDeployData(Service service, string containerName, ContainerInspectResponse inspect,
        string? environmentNetworkName)
    {
        string? rawIp = null;
        if (environmentNetworkName != null &&
            inspect.NetworkSettings.Networks.TryGetValue(environmentNetworkName, out var environmentEndpoint))
        {
            rawIp = environmentEndpoint.IPAddress;
        }

        rawIp ??= inspect.NetworkSettings.Networks.Values
            .Select(n => n.IPAddress)
            .FirstOrDefault(ip => !string.IsNullOrEmpty(ip));

        return new DeployData
        {
            ServiceId = service.Id,
            IpAddress = !string.IsNullOrEmpty(rawIp) ? IPAddress.Parse(rawIp) : null,
            ContainerName = containerName,
            Ports = inspect.ExtractPortMappings()
        };
    }

    public async Task HealTraefikRoutingBestEffortAsync(Guid serviceId, string? expectedIpAddress,
        CancellationToken cancellationToken)
    {
        try
        {
            await _traefikRoutingHealer.VerifyAndHealAsync(serviceId, expectedIpAddress, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Traefik routing self-heal check failed for service {ServiceId}; leaving as-is",
                serviceId);
        }
    }

    private static RestartPolicy MapRestartPolicy(Haven.Domain.Enums.RestartPolicy policy)
    {
        return policy switch
        {
            Haven.Domain.Enums.RestartPolicy.No => new RestartPolicy { Name = RestartPolicyKind.No },
            Haven.Domain.Enums.RestartPolicy.Always => new RestartPolicy { Name = RestartPolicyKind.Always },
            Haven.Domain.Enums.RestartPolicy.OnFailure => new RestartPolicy { Name = RestartPolicyKind.OnFailure },
            Haven.Domain.Enums.RestartPolicy.UnlessStopped => new RestartPolicy
            {
                Name = RestartPolicyKind.UnlessStopped
            },
            _ => new RestartPolicy() { Name = RestartPolicyKind.Undefined },
        };
    }
}