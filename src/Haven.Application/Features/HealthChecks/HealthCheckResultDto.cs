using Haven.Domain.Enums;

namespace Haven.Application.Features.HealthChecks;

public class HealthCheckResultDto
{
    public Guid Id { get; set; }
    public DateTime RanAt { get; set; }
    public ServiceHealth Status { get; set; }
    public HealthCheckFailureReason Reason { get; set; }
    public string? Message { get; set; }
    public long DurationMs { get; set; }
    public int? HttpStatusCode { get; set; }
    public long? ExitCode { get; set; }
    public string? Output { get; set; }
    public int Attempts { get; set; }
}
