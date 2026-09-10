using Docker.DotNet;
using Docker.DotNet.Models;

using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Shell;
using Haven.Domain.Enums;
using Haven.Infrastructure.Deployment.Docker;
using Haven.Infrastructure.Utils;

using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.Deployment.Docker.Shell;

/// <inheritdoc cref="IContainerShellService" />
public sealed class DockerContainerShellService : IContainerShellService
{
    private readonly IDockerClient _dockerClient;
    private readonly IDockerContainerRuntime _containerRuntime;
    private readonly ILogger<DockerContainerShellService> _logger;

    public DockerContainerShellService(
        IDockerClient dockerClient,
        IDockerContainerRuntime containerRuntime,
        ILogger<DockerContainerShellService> logger)
    {
        _dockerClient = dockerClient;
        _containerRuntime = containerRuntime;
        _logger = logger;
    }

    public async Task<Result<IShellSession>> CreateSessionAsync(Guid serviceId, ShellType shellType, CancellationToken cancellationToken)
    {
        var containers = await _containerRuntime.GetContainersByLabelAsync(DockerUtils.BuildIdLabel(serviceId), cancellationToken);

        var container = containers.FirstOrDefault();
        if (container is null)
        {
            _logger.LogWarning("No Docker container found for service '{ServiceId}'", serviceId);
            return Error.Docker.ContainerNotFound;
        }

        if (container.State != "running")
        {
            _logger.LogWarning("Container for service '{ServiceId}' is not running (state: '{State}')", serviceId, container.State);
            return Error.Docker.OperationFailed("The container must be running to open a shell.");
        }

        var execCreateResponse = await _dockerClient.Exec.ExecCreateContainerAsync(
            container.ID,
            new ContainerExecCreateParameters
            {
                AttachStdin = true,
                AttachStdout = true,
                AttachStderr = true,
                Tty = true,
                Cmd = [ShellCommand(shellType)]
            },
            cancellationToken);

        var stream = await _dockerClient.Exec.StartAndAttachContainerExecAsync(execCreateResponse.ID, true, cancellationToken);

        _logger.LogInformation("Opened interactive '{ShellType}' shell for service '{ServiceId}' (exec '{ExecId}')", shellType, serviceId, execCreateResponse.ID);

        return Result<IShellSession>.Success(new DockerShellSession(serviceId, shellType, stream));
    }

    private static string ShellCommand(ShellType shellType) => shellType switch
    {
        ShellType.Bash => "/bin/bash",
        ShellType.Sh => "/bin/sh",
        _ => throw new ArgumentOutOfRangeException(nameof(shellType), shellType, "Unsupported shell type.")
    };
}
