using Haven.Domain.Aggregates;
using Haven.Domain.Enums;

namespace Haven.Domain.Entities;

public class Deployment : Entity
{
    public Guid ServiceId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public DeploymentStatus Status { get; set; }
    public string? TriggeredBy { get; set; }
    public string LogFile { get; set; }

    public Service? Service { get; set; }

    private Deployment() { }

    public static Deployment Create(Guid serviceId, string logFile, string? triggeredBy = null)
    {
        return new Deployment
        {
            Id = Guid.NewGuid(),
            ServiceId = serviceId,
            StartedAt = DateTimeOffset.UtcNow,
            LogFile = logFile,
            TriggeredBy = triggeredBy,
            Status = DeploymentStatus.InProgress
        };
    }

    public void Complete()
    {
        FinishedAt = DateTimeOffset.UtcNow;
        Status = DeploymentStatus.Succeeded;
    }

    public void Fail()
    {
        FinishedAt = DateTimeOffset.UtcNow;
        Status = DeploymentStatus.Failed;
    }

    public void Cancel()
    {
        FinishedAt = DateTimeOffset.UtcNow;
        Status = DeploymentStatus.Cancelled;
    }
}