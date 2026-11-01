using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Templating;
using Haven.Application.Features.Services.ComputedOutputs;
using Haven.Domain.Aggregates;
using Haven.Infrastructure.Extensions;

namespace Haven.Infrastructure.Deployment;

public sealed class ServiceTemplateNamespaceProvider(
    IEnvironmentVariableService environmentVariableService,
    IFeatureFlagService featureFlagService,
    ISecretVariableService secretVariableService,
    IServiceRepository serviceRepository,
    IServiceRegistryEntryRepository serviceRegistryEntryRepository,
    IComputedOutputResolver computedOutputResolver) : IServiceTemplateNamespaceProvider
{
    public async Task<IReadOnlyDictionary<string, TemplateNamespaceResolver>?> BuildAsync(Guid serviceId,
        CancellationToken cancellationToken = default)
    {
        var service = await serviceRepository.GetByIdAsync(serviceId, cancellationToken);
        if (service is null)
            return null;

        // Only the other services in the same environment are exposed, addressed by alias.
        // Their own namespaces are built without a `services` namespace to avoid circular references.
        var serviceResolvers = new Dictionary<string, IReadOnlyDictionary<string, TemplateNamespaceResolver>>();
        var siblings = await serviceRepository.GetByEnvironmentIdAsync(service.EnvironmentId, cancellationToken);
        foreach (var sibling in siblings)
        {
            if (sibling.Id == service.Id || string.IsNullOrWhiteSpace(sibling.Alias))
                continue;

            serviceResolvers[sibling.Alias] = await BuildForServiceAsync(sibling, null, cancellationToken);
        }

        return await BuildForServiceAsync(service, serviceResolvers, cancellationToken);
    }

    private async Task<IReadOnlyDictionary<string, TemplateNamespaceResolver>> BuildForServiceAsync(Service service,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, TemplateNamespaceResolver>>? serviceResolvers,
        CancellationToken cancellationToken)
    {
        var serviceId = service.Id;
        var envs = await environmentVariableService.BuildVariablesForServiceAsync(serviceId, cancellationToken);
        var flags = await featureFlagService.GetFlagsAsEnvironmentsForServiceAsync(serviceId, cancellationToken);
        var secrets = await secretVariableService.GetSecretsAsEnvironmentVariablesForServiceAsync(serviceId, cancellationToken);

        var envsAndFlags = envs.Where(e => flags.All(f => f.Key != e.Key)).Concat(flags).ToList();

        var registry = await serviceRegistryEntryRepository.GetForServiceAsync(serviceId, cancellationToken);
        var resolved = await computedOutputResolver.ResolveAsync(service, registry, cancellationToken);

        var outputs = resolved.ToDictionary(r => r.Key, r => r.Value);
        var namespaces = service.ResolveTemplateNamespaces([], envsAndFlags, secrets.ToList(), outputs, serviceResolvers);

        return namespaces;
    }
}
