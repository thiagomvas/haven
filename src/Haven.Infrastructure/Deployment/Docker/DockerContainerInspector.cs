using Docker.DotNet;
using Docker.DotNet.Models;

using Haven.Application.Common;
using Haven.Infrastructure.Utils;

using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.Deployment.Docker;

/// <inheritdoc cref="IDockerContainerInspector" />
public sealed class DockerContainerInspector(
    IDockerClient dockerClient,
    ILogger<DockerContainerInspector> logger) : IDockerContainerInspector
{
    public Task<IList<ContainerListResponse>> GetContainersByLabelAsync(KeyValuePair<string, string> label,
        CancellationToken cancellationToken)
    {
        var param = new ContainersListParameters
        {
            All = true,
            Filters = new Dictionary<string, IDictionary<string, bool>>
            {
                { "label", new Dictionary<string, bool> { { $"{label.Key}={label.Value}", true } } }
            }
        };

        return dockerClient.Containers.ListContainersAsync(param, cancellationToken);
    }

    public async Task<Result<ContainerInspectResponse>> InspectByServiceIdAsync(Guid serviceId,
        CancellationToken cancellationToken)
    {
        var containers = await GetContainersByLabelAsync(DockerUtils.BuildIdLabel(serviceId), cancellationToken);

        var container = containers.FirstOrDefault();
        if (container is null)
        {
            logger.LogWarning("No Docker container found for service '{ServiceId}'", serviceId);
            return Error.Docker.ContainerNotFound;
        }

        return await dockerClient.Containers.InspectContainerAsync(container.ID, cancellationToken);
    }

    public async Task<Result> RestartByServiceIdAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        var containers = await GetContainersByLabelAsync(DockerUtils.BuildIdLabel(ownerId), cancellationToken);

        var container = containers.FirstOrDefault();
        if (container is null)
        {
            logger.LogWarning("No Docker container found for '{OwnerId}' to restart", ownerId);
            return Error.Docker.ContainerNotFound;
        }

        try
        {
            await dockerClient.Containers.RestartContainerAsync(container.ID, new ContainerRestartParameters(),
                cancellationToken);
            logger.LogInformation("Docker container '{ContainerId}' restarted (owner '{OwnerId}')", container.ID,
                ownerId);
            return Result.Success();
        }
        catch (DockerApiException ex)
        {
            logger.LogWarning(ex, "Failed to restart container '{ContainerId}' (owner '{OwnerId}')", container.ID,
                ownerId);
            return Error.Docker.OperationFailed(ex.Message);
        }
    }
}