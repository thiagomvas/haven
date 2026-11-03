using Haven.Domain;
using Haven.Domain.Enums;

namespace Haven.Application.Features.Deployments.Queries.GetRecentDeployments;

public sealed class RecentDeploymentDto
{
    public Guid Id { get; init; }
    public Guid ServiceId { get; init; }
    public string ServiceName { get; init; } = default!;
    public Guid ProjectId { get; init; }
    public string ProjectName { get; init; } = default!;
    public Guid EnvironmentId { get; init; }
    public string EnvironmentName { get; init; } = default!;
    public DeploymentStatus Status { get; init; }
    public string? TriggeredBy { get; init; }
    public DeploymentIdentifierType? IdentifierType { get; init; }
    public string? Identifier { get; init; }
    public string? CommitMessage { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? FinishedAt { get; init; }
}
