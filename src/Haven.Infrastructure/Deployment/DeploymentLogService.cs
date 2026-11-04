using System.Collections.Concurrent;

using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Hubs;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Configuration;
using Haven.Domain.Enums;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Haven.Infrastructure.Deployment;

public class DeploymentLogService(
    IServiceScopeFactory scopeFactory,
    IDeploymentLogNotifier notifier,
    IOptionsMonitor<InstanceOptions> instanceOptions) : IDeploymentLogService
{
    private readonly ConcurrentDictionary<Guid, LogWriter> _writers = new();

    public async Task<Domain.Entities.Deployment> QueueDeploymentForServiceAsync(Guid serviceId, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDeploymentRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var existing = await repository.FindActiveForServiceAsync(serviceId, ct);
        if (existing is not null) return existing;

        var deployment = Domain.Entities.Deployment.Create(serviceId, NewLogFilePath(serviceId));
        await repository.AddAsync(deployment, ct);
        await unitOfWork.SaveChangesAsync(ct);
        await PublishStatusAsync(scope, serviceId, ct);
        return deployment;
    }

    /// <summary>
    /// Deployment rows drive the displayed status (Queued/Deploying) but raise no domain events, so every
    /// lifecycle change pushes the recomputed status itself. Without this, a failed or cancelled deployment that
    /// leaves the runtime state unchanged would never tell clients it is over.
    /// </summary>
    private static Task PublishStatusAsync(IServiceScope scope, Guid serviceId, CancellationToken ct) =>
        scope.ServiceProvider.GetRequiredService<IServiceStatusPublisher>().PublishAsync(serviceId, ct);

    /// <summary>
    /// Starts the deployment for a service: adopts the one queued when the deploy was requested, or creates it
    /// when the deploy was started some other way. Opens the log file either way.
    /// </summary>
    public async Task<Domain.Entities.Deployment> CreateDeploymentForServiceAsync(Guid serviceId, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDeploymentRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var deployment = await repository.FindActiveForServiceAsync(serviceId, ct);
        if (deployment is null)
        {
            deployment = Domain.Entities.Deployment.Create(serviceId, NewLogFilePath(serviceId));
            await repository.AddAsync(deployment, ct);
        }

        // An in-flight deployment that is already past Queued (e.g. left behind by a crashed worker) is reused as-is.
        if (deployment.Status == DeploymentStatus.Queued)
            deployment.StartBuilding();
        await unitOfWork.SaveChangesAsync(ct);
        await PublishStatusAsync(scope, serviceId, ct);

        Directory.CreateDirectory(Path.GetDirectoryName(deployment.LogFile)!);
        var writer = new LogWriter(new StreamWriter(
            new FileStream(deployment.LogFile, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            AutoFlush = true
        });
        _writers[deployment.Id] = writer;

        return deployment;
    }

    private string NewLogFilePath(Guid serviceId) =>
        $"{instanceOptions.CurrentValue.DeploymentLogBasePath}/{serviceId}_{DateTime.UtcNow:yyyyMMddHHmmss}.log";

    public async Task MarkDeploymentStartingAsync(Guid deploymentId, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDeploymentRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var deployment = await repository.FindByIdAsync(deploymentId, ct);
        if (deployment is null || deployment.Status != DeploymentStatus.Building) return;

        deployment.StartStarting();
        await unitOfWork.SaveChangesAsync(ct);
        await PublishStatusAsync(scope, deployment.ServiceId, ct);
    }

    public async Task SetIdentifierAsync(
        Guid deploymentId,
        DeploymentIdentifierType type,
        string identifier,
        string? commitMessage,
        CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDeploymentRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var deployment = await repository.FindByIdAsync(deploymentId, ct);
        if (deployment is null || deployment.Status.IsTerminal) return;

        deployment.SetIdentifier(type, identifier, commitMessage);
        await unitOfWork.SaveChangesAsync(ct);
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

        // Callers normally move the deployment to Starting once the container is up; make sure it has before completing.
        if (deployment.Status != DeploymentStatus.Starting)
            deployment.StartStarting();
        deployment.Complete();
        await unitOfWork.SaveChangesAsync(ct);
        await PublishStatusAsync(scope, deployment.ServiceId, ct);
    }

    public async Task MarkDeploymentFailedAsync(Guid deploymentId, CancellationToken ct)
    {
        await FlushAndClose(deploymentId);

        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDeploymentRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var deployment = await repository.FindByIdAsync(deploymentId, ct);
        if (deployment is null) return;

        if (deployment.Status.IsTerminal) return;
        deployment.Fail();
        await unitOfWork.SaveChangesAsync(ct);
        await PublishStatusAsync(scope, deployment.ServiceId, ct);
    }

    public async Task MarkDeploymentCancelledAsync(Guid deploymentId, CancellationToken ct)
    {
        await FlushAndClose(deploymentId);

        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDeploymentRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var deployment = await repository.FindByIdAsync(deploymentId, ct);
        if (deployment is null) return;

        if (deployment.Status.IsTerminal) return;
        deployment.Cancel();
        await unitOfWork.SaveChangesAsync(ct);
        await PublishStatusAsync(scope, deployment.ServiceId, ct);
    }

    private async Task FlushAndClose(Guid deploymentId)
    {
        if (_writers.TryRemove(deploymentId, out var writer))
            await writer.DisposeAsync();
    }

    /// <summary>
    /// Serializes writes to a <see cref="StreamWriter"/>, which does not allow concurrent async operations.
    /// Callers (e.g. Docker build progress callbacks) may append from multiple threads at once.
    /// </summary>
    private sealed class LogWriter(StreamWriter inner)
    {
        private readonly SemaphoreSlim _gate = new(1, 1);
        private bool _disposed;

        public async Task WriteLineAsync(string line)
        {
            await _gate.WaitAsync();
            try
            {
                if (_disposed) return;
                await inner.WriteLineAsync(line);
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task DisposeAsync()
        {
            await _gate.WaitAsync();
            try
            {
                if (_disposed) return;
                _disposed = true;
                await inner.DisposeAsync();
            }
            finally
            {
                _gate.Release();
            }
        }
    }
}