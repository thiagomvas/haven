using System.Collections.Concurrent;

using Haven.Application.Common.Interfaces.Deployment;

namespace Haven.Infrastructure.Deployment;

public sealed class DeploymentCancellationService : IDeploymentCancellationService
{
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _sources = new();

    public CancellationToken Register(Guid serviceId)
    {
        var cts = new CancellationTokenSource();
        _sources[serviceId] = cts;
        return cts.Token;
    }

    public void Cancel(Guid serviceId)
    {
        if (_sources.TryGetValue(serviceId, out var cts))
            cts.Cancel();
    }

    public void Unregister(Guid serviceId)
    {
        if (_sources.TryRemove(serviceId, out var cts))
            cts.Dispose();
    }

    public bool IsRegistered(Guid serviceId) => _sources.ContainsKey(serviceId);
}
