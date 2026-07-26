using Docker.DotNet;
using Docker.DotNet.Models;

using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Domain;
using Haven.Domain.Entities;
using Haven.Infrastructure.Utils;

using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.Deployment;

/// <inheritdoc cref="IDockerContainerRuntime" />
public sealed class DockerContainerRuntime : IDockerContainerRuntime
{
    private readonly IDockerClient _dockerClient;
    private readonly ILogger<DockerContainerRuntime> _logger;

    public DockerContainerRuntime(IDockerClient dockerClient, ILogger<DockerContainerRuntime> logger)
    {
        _dockerClient = dockerClient;
        _logger = logger;
    }

    public CreateContainerParameters BuildContainerParameters(
        string name,
        IDictionary<string, string> labels,
        string image,
        IEnumerable<EnvironmentVariables>? envs,
        ExposureMode exposureMode,
        IReadOnlyList<string> ports,
        IList<Mount> mounts)
    {
        var envVars = DockerUtils.BuildEnvironmentVariableStrings(envs);
        var hostConfig = new HostConfig();
        var param = new CreateContainerParameters
        {
            Name = name,
            Labels = labels,
            Image = image,
        };

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

        param.HostConfig = hostConfig;

        if (envVars.Count > 0)
            param.Env = envVars;

        return param;
    }

    public async Task<Result<string>> CreateAndStartAsync(CreateContainerParameters parameters, CancellationToken cancellationToken)
    {
        var response = await _dockerClient.Containers.CreateContainerAsync(parameters, cancellationToken);

        var started = await _dockerClient.Containers.StartContainerAsync(response.ID, new ContainerStartParameters(), cancellationToken);

        if (!started)
        {
            _logger.LogError("Failed to start Docker container '{ContainerId}'", response.ID);
            return Error.Docker.FailedToStartContainer;
        }

        return response.ID;
    }

    public async Task ConnectToNetworksAsync(Guid ownerId, IReadOnlyCollection<Guid> networkIds, INetworkingService networkingService, CancellationToken cancellationToken)
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

    public Task<IList<ContainerListResponse>> GetContainersByLabelAsync(KeyValuePair<string, string> label, CancellationToken cancellationToken)
    {
        var param = new ContainersListParameters
        {
            All = true,
            Filters = new Dictionary<string, IDictionary<string, bool>>
            {
                {
                    "label",
                    new Dictionary<string, bool>
                    {
                        { $"{label.Key}={label.Value}", true }
                    }
                }
            }
        };

        return _dockerClient.Containers.ListContainersAsync(param, cancellationToken);
    }

    public async Task StopAndRemoveAsync(IReadOnlyCollection<ContainerListResponse> containers, Guid ownerId, INetworkingService networkingService, string reason, CancellationToken cancellationToken)
    {
        await networkingService.DisconnectServiceFromAllNetworksAsync(ownerId, cancellationToken);

        foreach (var container in containers)
        {
            if (container.State == "running")
            {
                try
                {
                    await _dockerClient.Containers.StopContainerAsync(container.ID, new ContainerStopParameters(), cancellationToken);
                }
                catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
                {
                    _logger.LogDebug("Timeout stopping container '{ContainerId}', proceeding with removal", container.ID);
                }
            }

            await _dockerClient.Containers.RemoveContainerAsync(container.ID, new ContainerRemoveParameters { Force = true }, cancellationToken);
            _logger.LogInformation("Docker container '{ContainerId}' {Reason} (owner '{OwnerId}')", container.ID, reason, ownerId);
        }
    }

    public async Task RemoveAllForOwnerAsync(Guid ownerId, INetworkingService networkingService, string reason, CancellationToken cancellationToken)
    {
        var containers = await GetContainersByLabelAsync(DockerUtils.BuildIdLabel(ownerId), cancellationToken);

        if (containers.Count > 0)
            await StopAndRemoveAsync((IReadOnlyCollection<ContainerListResponse>)containers, ownerId, networkingService, reason, cancellationToken);
    }
}