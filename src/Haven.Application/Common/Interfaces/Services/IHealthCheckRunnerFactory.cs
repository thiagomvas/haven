using Haven.Domain;
using Haven.Domain.Enums;

namespace Haven.Application.Common.Interfaces.Services;

public interface IHealthCheckRunnerFactory
{
    IHealthCheckRunner Create(HealthCheckKind kind);
}