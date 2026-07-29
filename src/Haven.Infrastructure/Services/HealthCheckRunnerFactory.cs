using Haven.Application.Common.Interfaces.Services;
using Haven.Domain;

namespace Haven.Infrastructure.Services;

public class HealthCheckRunnerFactory : IHealthCheckRunnerFactory
{
    private readonly IEnumerable<IHealthCheckRunner> _runners;

    public HealthCheckRunnerFactory(IEnumerable<IHealthCheckRunner> runners)
    {
        _runners = runners;
    }

    public IHealthCheckRunner Create(HealthCheckKind kind)
    {
        var runner = _runners.FirstOrDefault(r => r.Kind == kind);
        if (runner == null)
            throw new InvalidOperationException($"No health check runner found for kind {kind}.");
        return runner;
    }
}