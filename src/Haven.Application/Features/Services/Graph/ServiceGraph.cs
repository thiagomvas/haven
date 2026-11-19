using System.Runtime.CompilerServices;
using Haven.Application.Common.Templating;
using Environment = Haven.Domain.Aggregates.Environment;

namespace Haven.Application.Features.Services.Graph;

/// <summary>
/// The dependency graph between the services of an environment. A service depends on another one when any of
/// its environment variables, secrets or feature flag values references it through the <c>services</c> namespace,
/// e.g. <c>${{ services.db.outputs.connectionString }}</c>.
/// </summary>
public sealed class ServiceGraph
{
    public IReadOnlyList<ServiceNode> Nodes { get; }

    private ServiceGraph(IReadOnlyList<ServiceNode> nodes)
    {
        Nodes = nodes;
    }

    /// <summary>
    /// Builds the graph for <paramref name="environment"/>.
    /// </summary>
    /// <param name="environment">The environment whose services form the graph's nodes.</param>
    /// <param name="valuesByServiceId">
    /// The raw (unresolved) template-bearing values of each service, i.e. its environment variable, secret and
    /// feature flag values, keyed by service id. Services without an entry have no dependencies.
    /// </param>
    public static ServiceGraph FromEnvironment(
        Environment environment,
        IReadOnlyDictionary<Guid, IReadOnlyCollection<string?>> valuesByServiceId)
    {
        var nodes = environment.Services
            .Select(s => ServiceNode.FromService(s, new List<ServiceNode>()))
            .ToList();

        // Services are addressed by alias in templates; services without one can't be referenced.
        var nodesByAlias = nodes
            .Where(n => !string.IsNullOrWhiteSpace(n.Service.Alias))
            .GroupBy(n => n.Service.Alias!, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

        foreach (var node in nodes)
        {
            var values = valuesByServiceId.GetValueOrDefault(node.Service.Id) ?? [];
            BuildDependencies(node, nodesByAlias, values);
        }

        return new ServiceGraph(nodes);
    }

    /// <summary>
    /// Returns the services that directly depend on <paramref name="serviceId"/>, i.e. those that reference it.
    /// </summary>
    public IReadOnlyList<ServiceNode> GetDependants(Guid serviceId) =>
        Nodes
            .Where(n => n.Service.Id != serviceId && n.Dependencies.Any(d => d.Service.Id == serviceId))
            .ToList();

    private static void BuildDependencies(
        ServiceNode node,
        IReadOnlyDictionary<string, ServiceNode> nodesByAlias,
        IReadOnlyCollection<string?> values)
    {
        foreach (var value in values)
        {
            if (string.IsNullOrEmpty(value))
                continue;

            foreach (var key in TemplateExpressionResolver.FindKeys(value, TemplateNamespaces.Services))
            {
                var alias = key.Split('.', 2)[0];

                if (!nodesByAlias.TryGetValue(alias, out var target)
                    || ReferenceEquals(target, node)
                    || node.Dependencies.Contains(target, ReferenceNodeComparer.Instance))
                    continue;

                node.Dependencies.Add(target);
            }
        }
    }

    private sealed class ReferenceNodeComparer : IEqualityComparer<ServiceNode>
    {
        public static readonly ReferenceNodeComparer Instance = new();

        public bool Equals(ServiceNode? x, ServiceNode? y) => ReferenceEquals(x, y);

        public int GetHashCode(ServiceNode obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
