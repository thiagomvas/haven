using System.Net;

using Docker.DotNet;
using Docker.DotNet.Models;

using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Infrastructure.Utils;

using Microsoft.Extensions.Logging;

using RestartPolicy = Docker.DotNet.Models.RestartPolicy;

namespace Haven.Infrastructure.Deployment.Docker;

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
        IList<Mount> mounts,
        Haven.Domain.Enums.RestartPolicy restartPolicy)
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

        hostConfig.RestartPolicy = MapRestartPolicy(restartPolicy);
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

    public async Task EnsureNamedVolumesReadyAsync(string image, IEnumerable<Mount> mounts, CancellationToken cancellationToken)
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

            await _dockerClient.Volumes.CreateAsync(new VolumesCreateParameters { Name = mount.Source }, cancellationToken);

            string? user;
            try
            {
                var imageInspect = await _dockerClient.Images.InspectImageAsync(image, cancellationToken);
                user = imageInspect.Config?.User;
            }
            catch (DockerApiException ex)
            {
                _logger.LogWarning(ex, "Could not inspect image '{Image}' to determine ownership for volume '{Volume}'", image, mount.Source);
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
    private async Task ChownVolumeAsync(string image, string volumeName, string target, string user, CancellationToken cancellationToken)
    {
        var helperParams = new CreateContainerParameters
        {
            Image = image,
            User = "0:0",
            Entrypoint = new List<string> { "chown" },
            Cmd = new List<string> { "-R", user, target },
            HostConfig = new HostConfig
            {
                Mounts = new List<Mount> { new Mount { Type = "volume", Source = volumeName, Target = target } },
                AutoRemove = true
            }
        };

        try
        {
            var created = await _dockerClient.Containers.CreateContainerAsync(helperParams, cancellationToken);
            await _dockerClient.Containers.StartContainerAsync(created.ID, new ContainerStartParameters(), cancellationToken);
            await _dockerClient.Containers.WaitContainerAsync(created.ID, cancellationToken);
            _logger.LogInformation("Fixed ownership of named volume '{Volume}' to '{User}' for image '{Image}'", volumeName, user, image);
        }
        catch (DockerApiException ex)
        {
            _logger.LogWarning(ex, "Failed to fix ownership of named volume '{Volume}' for image '{Image}'; container may fail to start if it requires non-root write access", volumeName, image);
        }
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

    public async Task<Result<ContainerInspectResponse>> InspectByServiceIdAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        var containers = await GetContainersByLabelAsync(DockerUtils.BuildIdLabel(serviceId), cancellationToken);

        var container = containers.FirstOrDefault();
        if (container is null)
        {
            _logger.LogWarning("No Docker container found for service '{ServiceId}'", serviceId);
            return Error.Docker.ContainerNotFound;
        }

        return await _dockerClient.Containers.InspectContainerAsync(container.ID, cancellationToken);
    }

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

        using var stream = await _dockerClient.Exec.StartAndAttachContainerExecAsync(execCreateResponse.ID, false, linkedCts.Token);
        var (stdout, stderr) = await stream.ReadOutputToEndAsync(linkedCts.Token);

        var inspectResponse = await _dockerClient.Exec.InspectContainerExecAsync(execCreateResponse.ID, cancellationToken);

        return (inspectResponse.ExitCode, stdout, stderr);
    }

    private static RestartPolicy MapRestartPolicy(Haven.Domain.Enums.RestartPolicy policy)
    {
        return policy switch
        {
            Haven.Domain.Enums.RestartPolicy.No => new RestartPolicy { Name = RestartPolicyKind.No },
            Haven.Domain.Enums.RestartPolicy.Always => new RestartPolicy { Name = RestartPolicyKind.Always },
            Haven.Domain.Enums.RestartPolicy.OnFailure => new RestartPolicy { Name = RestartPolicyKind.OnFailure },
            Haven.Domain.Enums.RestartPolicy.UnlessStopped => new RestartPolicy { Name = RestartPolicyKind.UnlessStopped },
            _ => new RestartPolicy() { Name = RestartPolicyKind.Undefined },
        };
    }
}