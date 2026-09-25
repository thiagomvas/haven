using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Entities;
using Haven.Domain.Enums;

namespace Haven.Infrastructure.Deployment;

public class SecretVariableService(
    IEnvironmentRepository environmentRepository,
    IServiceRepository serviceRepository,
    ISecretVariableRepository secretVariableRepository) : ISecretVariableService
{
    public async Task<IEnumerable<EnvironmentVariables>> GetSecretsAsEnvironmentVariablesForServiceAsync(Guid serviceId, CancellationToken cancellationToken = default)
    {
        var service = await serviceRepository.GetByIdAsync(serviceId, cancellationToken);
        if (service is null || service.Environment is null)
        {
            return [];
        }

        var environmentSecrets = await GetSecretsForEnvironmentAsync(service.Environment.Id, cancellationToken);
        var serviceSecrets = await GetSecretsForParentAsync(serviceId, EnvironmentVariableParentType.Service, cancellationToken);

        return Merge(environmentSecrets, serviceSecrets);
    }

    private async Task<List<EnvironmentVariables>> GetSecretsForEnvironmentAsync(Guid environmentId, CancellationToken cancellationToken)
    {
        var environment = await environmentRepository.GetByIdAsync(environmentId, cancellationToken);
        if (environment is null)
        {
            return [];
        }

        var projectSecrets = await GetSecretsForParentAsync(environment.ProjectId, EnvironmentVariableParentType.Project, cancellationToken);
        var environmentSecrets = await GetSecretsForParentAsync(environmentId, EnvironmentVariableParentType.Environment, cancellationToken);

        return Merge(projectSecrets, environmentSecrets);
    }

    private async Task<List<EnvironmentVariables>> GetSecretsForParentAsync(Guid parentId, EnvironmentVariableParentType parentType, CancellationToken cancellationToken)
    {
        var secrets = await secretVariableRepository.GetForParentAsync(parentId, parentType, cancellationToken);

        return secrets.Select(s => new EnvironmentVariables
        {
            Key = s.Key,
            Value = s.Value is null ? string.Empty : s.Value.Value
        }).ToList();
    }

    private static List<EnvironmentVariables> Merge(IEnumerable<EnvironmentVariables> @base,
        IEnumerable<EnvironmentVariables> overrides)
    {
        var dict = @base.ToDictionary(x => x.Key, x => x);

        foreach (var dest in overrides)
        {
            dict[dest.Key] = dest;
        }

        return dict.Values.ToList();
    }
}
