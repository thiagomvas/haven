using Docker.DotNet;
using Docker.DotNet.Models;

using Haven.Infrastructure.Deployment.Docker;

using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.Deployment.Events;

public interface IDockerEventParser
{
    Task<DockerEvent?> ParseAsync(Message message, CancellationToken ct);
}

public class DockerEventParser : IDockerEventParser
{
    private readonly IDockerClient _dockerClient;
    private readonly ILogger<DockerEventParser> _logger;

    public DockerEventParser(IDockerClient dockerClient, ILogger<DockerEventParser> logger)
    {
        _dockerClient = dockerClient;
        _logger = logger;
    }

    public async Task<DockerEvent?> ParseAsync(Message message, CancellationToken ct)
    {
        var containerId = message.Actor?.ID;
        if (string.IsNullOrEmpty(containerId))
        {
            _logger.LogDebug("Docker event has no container ID");
            return null;
        }

        if (!TryExtractServiceId(message, out var serviceId))
        {
            serviceId = await TryInspectContainerAsync(containerId, ct);
            if (serviceId == null)
            {
                _logger.LogDebug("Container {ContainerId} has no haven.service.id label, skipping", containerId);
                return null;
            }
        }

        DockerEvent @event = message.Action switch
        {
            DockerEventTypes.Start => new ContainerStartedEvent(containerId, GetTimestamp(message), serviceId.Value),
            DockerEventTypes.Stop => new ContainerStoppedEvent(containerId, GetTimestamp(message), serviceId.Value),
            DockerEventTypes.Kill => new ContainerKilledEvent(containerId, GetTimestamp(message), serviceId.Value),
            DockerEventTypes.Die => new ContainerDiedEvent(containerId, GetTimestamp(message), serviceId.Value),
            DockerEventTypes.Health.Unhealthy => new ContainerUnhealthyEvent(containerId, GetTimestamp(message), serviceId.Value),
            DockerEventTypes.Health.Healthy => new ContainerHealthyEvent(containerId, GetTimestamp(message), serviceId.Value),
            DockerEventTypes.Oom => new ContainerOutOfMemoryEvent(containerId, GetTimestamp(message), serviceId.Value),
            _ => null
        };

        return @event;
    }

    private static bool TryExtractServiceId(Message message, out Guid? serviceId)
    {
        serviceId = null;

        if (message.Actor?.Attributes is null ||
            !message.Actor.Attributes.TryGetValue("haven.service.id", out var serviceIdStr) ||
            !Guid.TryParse(serviceIdStr, out var id))
        {
            return false;
        }

        serviceId = id;
        return true;
    }

    private async Task<Guid?> TryInspectContainerAsync(string containerId, CancellationToken ct)
    {
        try
        {
            var container = await _dockerClient.Containers.InspectContainerAsync(containerId, ct);
            if (container.Config?.Labels is not null &&
                container.Config.Labels.TryGetValue("haven.service.id", out var serviceIdStr) &&
                Guid.TryParse(serviceIdStr, out var serviceId))
            {
                return serviceId;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to inspect container {ContainerId}", containerId);
        }

        return null;
    }

    private static DateTime GetTimestamp(Message message) =>
        message.TimeNano > 0
            ? DateTime.UnixEpoch.AddTicks(message.TimeNano / 100)
            : DateTime.UtcNow;
}