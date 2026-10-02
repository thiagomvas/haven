using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Templating;
using Haven.Domain.Entities;
using Haven.Infrastructure.Extensions;

namespace Haven.Infrastructure.Deployment;

public class ContainerEnvironmentService(
    IEnvironmentVariableService environmentVariableService,
    IFeatureFlagService featureFlagService,
    ISecretVariableService secretVariableService,
    IServiceRepository serviceRepository) : IContainerEnvironmentService
{
    public async Task<IEnumerable<EnvironmentVariables>> BuildEnvironmentVariablesAsync(Guid serviceId,
        CancellationToken cancellationToken = default)
    {
        var envs = await environmentVariableService.BuildVariablesForServiceAsync(serviceId, cancellationToken);
        var flags = await featureFlagService.GetFlagsAsEnvironmentsForServiceAsync(serviceId, cancellationToken);
        var secrets = await secretVariableService.GetSecretsAsEnvironmentVariablesForServiceAsync(serviceId, cancellationToken);

        var envsAndFlags = MergeEnvironmentVariables(envs, flags).ToList();
        var secretsList = secrets.ToList();
        var merged = MergeEnvironmentVariables(envsAndFlags, secretsList).ToList();

        var service = await serviceRepository.GetByIdAsync(serviceId, cancellationToken);
        if (service is null)
            return merged;

        var namespaces = service.ResolveTemplateNamespaces([], envsAndFlags, secretsList);

        return merged.ConvertAll(e => new EnvironmentVariables
        {
            ParentId = e.ParentId,
            ParentType = e.ParentType,
            Key = e.Key,
            Value = e.Value is null ? null : TemplateExpressionResolver.Resolve(e.Value, namespaces)
        });
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