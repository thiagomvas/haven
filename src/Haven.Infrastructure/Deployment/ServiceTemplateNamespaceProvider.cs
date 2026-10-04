using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Templating;
using Haven.Infrastructure.Extensions;

namespace Haven.Infrastructure.Deployment;

public sealed class ServiceTemplateNamespaceProvider(
    IEnvironmentVariableService environmentVariableService,
    IFeatureFlagService featureFlagService,
    ISecretVariableService secretVariableService,
    IServiceRepository serviceRepository) : IServiceTemplateNamespaceProvider
{
    public async Task<IReadOnlyDictionary<string, TemplateNamespaceResolver>?> BuildAsync(Guid serviceId,
        CancellationToken cancellationToken = default)
    {
        var service = await serviceRepository.GetByIdAsync(serviceId, cancellationToken);
        if (service is null)
            return null;

        var envs = await environmentVariableService.BuildVariablesForServiceAsync(serviceId, cancellationToken);
        var flags = await featureFlagService.GetFlagsAsEnvironmentsForServiceAsync(serviceId, cancellationToken);
        var secrets = await secretVariableService.GetSecretsAsEnvironmentVariablesForServiceAsync(serviceId, cancellationToken);

        // Flags override variables of the same key, matching ContainerEnvironmentService.
        var envsAndFlags = envs.Where(e => flags.All(f => f.Key != e.Key)).Concat(flags).ToList();

        return service.ResolveTemplateNamespaces([], envsAndFlags, secrets.ToList());
    }
}
