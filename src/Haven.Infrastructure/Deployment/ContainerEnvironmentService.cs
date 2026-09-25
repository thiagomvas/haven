using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Domain.Entities;

namespace Haven.Infrastructure.Deployment;

public class ContainerEnvironmentService(
    IEnvironmentVariableService environmentVariableService,
    IFeatureFlagService featureFlagService,
    ISecretVariableService secretVariableService) : IContainerEnvironmentService
{
    public async Task<IEnumerable<EnvironmentVariables>> BuildEnvironmentVariablesAsync(Guid serviceId,
        CancellationToken cancellationToken = default)
    {
        var envs = await environmentVariableService.BuildVariablesForServiceAsync(serviceId, cancellationToken);
        var flags = await featureFlagService.GetFlagsAsEnvironmentsForServiceAsync(serviceId, cancellationToken);
        var secrets = await secretVariableService.GetSecretsAsEnvironmentVariablesForServiceAsync(serviceId, cancellationToken);

        var merged = MergeEnvironmentVariables(envs, flags);
        merged = MergeEnvironmentVariables(merged, secrets);

        return merged;
    }

    private IEnumerable<EnvironmentVariables> MergeEnvironmentVariables(IEnumerable<EnvironmentVariables> @base,
        IEnumerable<EnvironmentVariables> newOrOverrides)
    {
        var merged = new List<EnvironmentVariables>(@base);

        foreach (var flag in newOrOverrides)
        {
            var existing = merged.FirstOrDefault(e => e.Key == flag.Key);
            if (existing != null)
            {
                existing.Value = flag.Value;
            }
            else
            {
                merged.Add(flag);
            }
        }

        return merged;
    }
}