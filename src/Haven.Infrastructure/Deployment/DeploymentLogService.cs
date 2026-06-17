using System.Collections.Concurrent;

using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;

namespace Haven.Infrastructure.Deployment;

public class DeploymentLogService(IDeploymentRepository repository) : IDeploymentLogService
{
    private readonly ConcurrentDictionary<Guid, StreamWriter> _writers = new();

    private const string BaseLogFilePath = "/var/log/haven/deployments";
    public async Task<Domain.Entities.Deployment> CreateDeploymentForServiceAsync(Guid serviceId, CancellationToken ct)
    {
        var logFilePath = $"{BaseLogFilePath}/{serviceId}_{DateTime.UtcNow:yyyyMMddHHmmss}.log";
        var deployment = Domain.Entities.Deployment.Create(serviceId, logFilePath);
        await repository.AddAsync(deployment, ct);

        Directory.CreateDirectory(BaseLogFilePath);
        await using var _ = File.Create(logFilePath);
        
        return deployment;
    }

    public Task AppendLogAsync(Guid deploymentId, string logEntry, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public async Task MarkDeploymentCompletedAsync(Guid deploymentId, CancellationToken ct)
    {
        await FlushAndClose(deploymentId);
        var deployment = await repository.FindByIdAsync(deploymentId, ct);
        if (deployment is null) return;
        
        deployment.Complete();
    }

    public async Task MarkDeploymentFailedAsync(Guid deploymentId, CancellationToken ct)
    {
        await FlushAndClose(deploymentId);
        var deployment = await repository.FindByIdAsync(deploymentId, ct);
        if (deployment is null) return;
        
        deployment.Fail();
    }
    private Task FlushAndClose(Guid deploymentId)
    {
        if (_writers.TryRemove(deploymentId, out var writer))
            writer.Dispose();

        return Task.CompletedTask;
    }
}