using Haven.Domain;

namespace Haven.Application.Common.Interfaces.Services;

public interface IHealthCheckRunnerFactory
{
    IHealthCheckRunner Create(HealthCheckKind kind);
}