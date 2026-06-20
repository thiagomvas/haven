namespace Haven.Application.Common.Interfaces.Hubs;

public interface IDeploymentLogNotifier
{
    Task SendLogEntryAsync(Guid deploymentId, string message, CancellationToken ct = default);
}