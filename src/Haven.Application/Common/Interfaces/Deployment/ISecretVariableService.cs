using Haven.Domain.Entities;

namespace Haven.Application.Common.Interfaces.Deployment;

public interface ISecretVariableService
{
    Task<IEnumerable<EnvironmentVariables>> GetSecretsAsEnvironmentVariablesForServiceAsync(Guid serviceId, CancellationToken cancellationToken = default);
}