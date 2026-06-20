using Haven.Domain;

namespace Haven.Application.Features.Deployments;

public sealed class DeploymentDto
{
    public Guid Id { get; init; }
    public Guid ServiceId { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? FinishedAt { get; init; }
    public DeploymentStatus Status { get; init; }
    public string? TriggeredBy { get; init; }
}