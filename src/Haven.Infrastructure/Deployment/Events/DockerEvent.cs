using Mediator;

namespace Haven.Infrastructure.Deployment.Events;

public abstract record DockerEvent(string ContainerId, DateTime Timestamp) : INotification;

public record ContainerStartedEvent(string ContainerId, DateTime Timestamp, Guid ServiceId) : DockerEvent(ContainerId, Timestamp);

public record ContainerHealthyEvent(string ContainerId, DateTime Timestamp, Guid ServiceId) : DockerEvent(ContainerId, Timestamp);

public record ContainerStoppedEvent(string ContainerId, DateTime Timestamp, Guid ServiceId) : DockerEvent(ContainerId, Timestamp);

public record ContainerKilledEvent(string ContainerId, DateTime Timestamp, Guid ServiceId) : DockerEvent(ContainerId, Timestamp);

public record ContainerDiedEvent(string ContainerId, DateTime Timestamp, Guid ServiceId) : DockerEvent(ContainerId, Timestamp);

public record ContainerUnhealthyEvent(string ContainerId, DateTime Timestamp, Guid ServiceId) : DockerEvent(ContainerId, Timestamp);

public record ContainerOutOfMemoryEvent(string ContainerId, DateTime Timestamp, Guid ServiceId) : DockerEvent(ContainerId, Timestamp);