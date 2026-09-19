using Haven.Domain.Enums;

namespace Haven.Application.Features.Services;

/// <summary>The failing health check of a service that just became unhealthy, and why it failed.</summary>
public sealed record ServiceHealthIssueDto(string CheckName, HealthCheckFailureReason Reason, string? Message);
