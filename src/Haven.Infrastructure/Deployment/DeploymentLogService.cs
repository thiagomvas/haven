using System.Collections.Concurrent;

using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Hubs;
using Haven.Application.Common.Interfaces.Repositories;

using Microsoft.Extensions.DependencyInjection;

namespace Haven.Infrastructure.Deployment;

public class DeploymentLogService(IServiceScopeFactory scopeFactory, IDeploymentLogNotifier notifier) : IDeploymentLogService
{
    private readonly ConcurrentDictionary<Guid, StreamWriter> _writers = new();

    private const string BaseLogFilePath = "/home/thiagomv/haven/deployments";

    public async Task<Domain.Entities.Deployment> CreateDeploymentForServiceAsync(Guid serviceId, CancellationToken ct)
    {
        var logFilePath = $"{BaseLogFilePath}/{serviceId}_{DateTime.UtcNow:yyyyMMddHHmmss}.log";
        var deployment = Domain.Entities.Deployment.Create(serviceId, logFilePath);

        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDeploymentRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await repository.AddAsync(deployment, ct);
        await unitOfWork.SaveChangesAsync(ct);

        Directory.CreateDirectory(BaseLogFilePath);
        var writer = new StreamWriter(
            new FileStream(logFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            AutoFlush = true
        };
        _writers[deployment.Id] = writer;

        return deployment;
    }

    public async Task AppendLogAsync(Guid deploymentId, string logEntry, CancellationToken ct)
    {
        var timestamp = DateTime.UtcNow;

        if (_writers.TryGetValue(deploymentId, out var writer))
            await writer.WriteLineAsync($"[{timestamp:yyyy-MM-dd HH:mm:ss}] {logEntry}");

        await notifier.SendLogEntryAsync(deploymentId, logEntry, ct);
    }

    public async Task MarkDeploymentCompletedAsync(Guid deploymentId, CancellationToken ct)
    {
        await FlushAndClose(deploymentId);

        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDeploymentRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var deployment = await repository.FindByIdAsync(deploymentId, ct);
        if (deployment is null) return;

        deployment.Complete();
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task MarkDeploymentFailedAsync(Guid deploymentId, CancellationToken ct)
    {
        await FlushAndClose(deploymentId);

        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDeploymentRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var deployment = await repository.FindByIdAsync(deploymentId, ct);
        if (deployment is null) return;

        deployment.Fail();
        await unitOfWork.SaveChangesAsync(ct);
    }

    private Task FlushAndClose(Guid deploymentId)
    {
        if (_writers.TryRemove(deploymentId, out var writer))
            writer.Dispose();

        return Task.CompletedTask;
    }
}
