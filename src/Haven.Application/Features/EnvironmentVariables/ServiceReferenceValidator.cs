using Haven.Application.Common;
using Haven.Application.Common.Templating;
using Haven.Domain.Aggregates;

namespace Haven.Application.Features.EnvironmentVariables;

/// <summary>
/// Validates <c>${{ services.&lt;alias&gt;.&lt;scope&gt;.&lt;key&gt; }}</c> references in a service's environment
/// variables: the target service must exist in the same environment, a referenced output must be defined on it,
/// and the references between services must not form a cycle.
/// </summary>
public static class ServiceReferenceValidator
{
    private readonly record struct Reference(string Alias, string? Scope, string? Key, string Raw);

    /// <summary>
    /// Validates the environment variable content for a service, returning an error if any of the following conditions are violated:
    /// </summary>
    /// <param name="service">The service whose environment variables are being saved.</param>
    /// <param name="newContent">The environment variable content about to be saved for <paramref name="service"/>.</param>
    /// <param name="servicesInEnvironment">All services in the same environment (may include <paramref name="service"/>).</param>
    /// <param name="contentByServiceId">Currently saved environment variable content of the other services.</param>
    public static Error? Validate(
        Service service,
        string newContent,
        IReadOnlyCollection<Service> servicesInEnvironment,
        IReadOnlyDictionary<Guid, string> contentByServiceId)
    {
        var references = ParseReferences(newContent);
        if (references.Count == 0)
            return null;

        var byAlias = servicesInEnvironment
            .Where(s => s.Id != service.Id && !string.IsNullOrWhiteSpace(s.Alias))
            .GroupBy(s => s.Alias!)
            .ToDictionary(g => g.Key, g => g.First());

        foreach (var reference in references)
        {
            if (string.Equals(reference.Alias, service.Alias, StringComparison.Ordinal))
                return Error.Validation($"Service '{service.Alias}' cannot reference itself ('{reference.Raw}').");

            if (!byAlias.TryGetValue(reference.Alias, out var target))
                return Error.Validation(
                    $"'{reference.Raw}' references service '{reference.Alias}', which does not exist in this environment.");

            if (reference.Scope is null || string.IsNullOrEmpty(reference.Key))
                return Error.Validation(
                    $"'{reference.Raw}' is malformed; expected services.<alias>.<scope>.<key>.");

            if (reference.Scope == TemplateNamespaces.Outputs
                && target.ComputedProperties.All(p => p.Key != reference.Key))
                return Error.Validation(
                    $"'{reference.Raw}' references output '{reference.Key}', which is not defined on service '{reference.Alias}'.");
        }

        return DetectCycle(service, references, servicesInEnvironment, contentByServiceId);
    }

    private static Error? DetectCycle(
        Service service,
        List<Reference> references,
        IReadOnlyCollection<Service> servicesInEnvironment,
        IReadOnlyDictionary<Guid, string> contentByServiceId)
    {
        var idByAlias = servicesInEnvironment
            .Where(s => !string.IsNullOrWhiteSpace(s.Alias))
            .GroupBy(s => s.Alias!)
            .ToDictionary(g => g.Key, g => g.First().Id);
        var aliasById = servicesInEnvironment
            .Where(s => !string.IsNullOrWhiteSpace(s.Alias))
            .ToDictionary(s => s.Id, s => s.Alias!);

        var edges = new Dictionary<Guid, HashSet<Guid>>();
        foreach (var (id, content) in contentByServiceId)
        {
            if (id == service.Id) continue;
            edges[id] = ParseReferences(content)
                .Where(r => idByAlias.ContainsKey(r.Alias))
                .Select(r => idByAlias[r.Alias])
                .ToHashSet();
        }

        edges[service.Id] = references
            .Where(r => idByAlias.ContainsKey(r.Alias))
            .Select(r => idByAlias[r.Alias])
            .ToHashSet();

        var path = new List<Guid> { service.Id };
        var visited = new HashSet<Guid>();

        bool Visit(Guid current)
        {
            foreach (var next in edges.GetValueOrDefault(current) ?? [])
            {
                if (next == service.Id)
                    return true;
                if (!visited.Add(next))
                    continue;

                path.Add(next);
                if (Visit(next))
                    return true;
                path.RemoveAt(path.Count - 1);
            }

            return false;
        }

        if (!Visit(service.Id))
            return null;

        var chain = string.Join(" -> ", path.Append(service.Id).Select(id => aliasById.GetValueOrDefault(id, id.ToString())));
        return Error.Validation($"Circular service reference detected: {chain}.");
    }

    private static List<Reference> ParseReferences(string content)
    {
        return TemplateExpressionResolver.FindKeys(content, TemplateNamespaces.Services)
            .Select(raw =>
            {
                var parts = raw.Split('.', 3);
                return new Reference(
                    parts[0],
                    parts.Length > 1 && parts[1].Length > 0 ? parts[1] : null,
                    parts.Length > 2 ? parts[2] : null,
                    $"services.{raw}");
            })
            .ToList();
    }
}
