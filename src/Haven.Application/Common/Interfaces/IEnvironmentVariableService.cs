using Haven.Domain.Entities;

namespace Haven.Application.Common.Interfaces;

public interface IEnvironmentVariableService
{
    Task<IEnumerable<EnvironmentVariables>> BuildVariablesForServiceAsync(Guid serviceId, CancellationToken cancellationToken);
    Task<IEnumerable<EnvironmentVariables>> BuildVariablesForEnvironmentAsync(Guid environmentId, CancellationToken cancellationToken);
    Task<IEnumerable<EnvironmentVariables>> BuildVariablesForProjectAsync(Guid projectId, CancellationToken cancellationToken);
    Task<string> BuildEnvFileForServiceAsync(Guid serviceId, CancellationToken cancellationToken);
    Task<string> BuildEnvFileForEnvironmentAsync(Guid environmentId, CancellationToken cancellationToken);
    Task<string> BuildEnvFileForProjectAsync(Guid projectId, CancellationToken cancellationToken);
    Task<string> BuildEnvFileForServiceDirectAsync(Guid serviceId, CancellationToken cancellationToken);
    Task<string> BuildEnvFileForEnvironmentDirectAsync(Guid environmentId, CancellationToken cancellationToken);

    Task SetEnvironmentVariablesFromFileForProjectAsync(Guid projectId, string content,
        CancellationToken cancellationToken);
}