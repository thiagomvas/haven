using Haven.Domain.Entities;

namespace Haven.Application.Common.Interfaces;

public interface IEnvironmentVariableService
{
    Task<List<EnvironmentVariables>> BuildVariablesForServiceAsync(Guid serviceId, CancellationToken cancellationToken);
    Task<List<EnvironmentVariables>> BuildVariablesForEnvironmentAsync(Guid environmentId, CancellationToken cancellationToken);
    Task<List<EnvironmentVariables>> BuildVariablesForProjectAsync(Guid projectId, CancellationToken cancellationToken);
    Task<string> BuildEnvFileForServiceAsync(Guid serviceId, CancellationToken cancellationToken);
    Task<string> BuildEnvFileForEnvironmentAsync(Guid environmentId, CancellationToken cancellationToken);
    Task<string> BuildEnvFileForProjectAsync(Guid projectId, CancellationToken cancellationToken);
    Task<string> BuildEnvFileForServiceDirectAsync(Guid serviceId, CancellationToken cancellationToken);
    Task<string> BuildEnvFileForEnvironmentDirectAsync(Guid environmentId, CancellationToken cancellationToken);

    Task SetEnvironmentVariablesFromFileForProjectAsync(Guid projectId, string content,
        CancellationToken cancellationToken);
    
    Task SetEnvironmentVariablesFromFileForEnvironmentAsync(Guid environmentId, string content,
        CancellationToken cancellationToken);
    
    Task SetEnvironmentVariablesFromFileForServiceAsync(Guid serviceId, string content,
        CancellationToken cancellationToken);
}