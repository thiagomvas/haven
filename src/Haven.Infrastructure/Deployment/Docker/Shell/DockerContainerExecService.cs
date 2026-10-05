using Docker.DotNet;
using Docker.DotNet.Models;

using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Shell;
using Haven.Infrastructure.Utils;

using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.Deployment.Docker.Shell;

/// <inheritdoc cref="IContainerExecService" />
public sealed class DockerContainerExecService(
    IDockerClient dockerClient,
    IDockerContainerRuntime containerRuntime,
    ILogger<DockerContainerExecService> logger) : IContainerExecService
{
    public async Task<Result<ContainerExecResult>> ExecAsync(Guid serviceId, IReadOnlyList<string> command,
        string? workingDir, string? user, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var containers = await containerRuntime.GetContainersByLabelAsync(DockerUtils.BuildIdLabel(serviceId),
            cancellationToken);

        var container = containers.FirstOrDefault();
        if (container is null)
        {
            logger.LogWarning("No Docker container found for service '{ServiceId}'", serviceId);
            return Error.Docker.ContainerNotFound;
        }

        if (container.State != "running")
        {
            logger.LogWarning("Container for service '{ServiceId}' is not running (state: '{State}')", serviceId,
                container.State);
            return Error.Docker.OperationFailed("The container must be running to execute a command.");
        }

        using var timeoutCts = new CancellationTokenSource();
        if (timeout > TimeSpan.Zero)
            timeoutCts.CancelAfter(timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        var execCreateResponse = await dockerClient.Exec.ExecCreateContainerAsync(
            container.ID,
            new ContainerExecCreateParameters
            {
                AttachStdout = true,
                AttachStderr = true,
                Cmd = command.ToList(),
                WorkingDir = workingDir,
                User = user
            },
            linkedCts.Token);

        using var stream =
            await dockerClient.Exec.StartAndAttachContainerExecAsync(execCreateResponse.ID, false, linkedCts.Token);
        var (stdout, stderr) = await stream.ReadOutputToEndAsync(linkedCts.Token);

        var inspect = await dockerClient.Exec.InspectContainerExecAsync(execCreateResponse.ID, cancellationToken);

        return new ContainerExecResult(inspect.ExitCode, stdout, stderr);
    }
}