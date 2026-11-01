using Haven.Application.Common.Templating;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Infrastructure.Utils;

namespace Haven.Infrastructure.Extensions;

public static class TemplatingExtensions
{
    public static Dictionary<string, TemplateNamespaceResolver> ResolveTemplateNamespaces(this Service service, List<EnvironmentVariables>? environmentVariables = null)
        => service.ResolveTemplateNamespaces([], environmentVariables ?? []);

    public static Dictionary<string, TemplateNamespaceResolver> ResolveTemplateNamespaces(this Service service,
        Dictionary<string, string> inputValues, List<EnvironmentVariables> environmentVariables,
        List<EnvironmentVariables>? secrets = null,
        IReadOnlyDictionary<string, string>? outputs = null,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, TemplateNamespaceResolver>>? serviceResolvers = null)
    {

        return new Dictionary<string, TemplateNamespaceResolver>()
        {
            [TemplateNamespaces.Inputs] = inputValues.GetValueOrDefault,
            [TemplateNamespaces.Runtime] = service.ResolveRuntimeVariable,
            [TemplateNamespaces.EnvVariables] = key => environmentVariables.FirstOrDefault(e => e.Key == key)?.Value,
            [TemplateNamespaces.Secrets] = key => secrets?.FirstOrDefault(e => e.Key == key)?.Value,
            [TemplateNamespaces.Outputs] = key => outputs?.GetValueOrDefault(key),
            [TemplateNamespaces.Services] = key => ResolveServiceVariable(serviceResolvers, key),
        };
    }

    /// <summary>
    /// Resolves keys of the form <c>&lt;service-alias&gt;.&lt;scope&gt;.&lt;key&gt;</c>
    /// (e.g. <c>db.outputs.connectionString</c>) against the other services' namespace resolvers.
    /// </summary>
    private static string? ResolveServiceVariable(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, TemplateNamespaceResolver>>? serviceResolvers,
        string key)
    {
        if (serviceResolvers is null)
            return null;

        var aliasEnd = key.IndexOf('.');
        if (aliasEnd <= 0)
            return null;

        var scopeEnd = key.IndexOf('.', aliasEnd + 1);
        if (scopeEnd <= aliasEnd + 1 || scopeEnd == key.Length - 1)
            return null;

        var alias = key[..aliasEnd];
        var scope = key[(aliasEnd + 1)..scopeEnd];
        var innerKey = key[(scopeEnd + 1)..];

        return serviceResolvers.TryGetValue(alias, out var scopes) && scopes.TryGetValue(scope, out var resolve)
            ? resolve(innerKey)
            : null;
    }

    private static string? ResolveRuntimeVariable(this Service service, string key)
    {
        return key switch
        {
            "id" => service.Id.ToString(),
            "alias" => service.Alias,
            "name" => service.Name,
            "environment" => service.Environment?.Name,
            "project" => service.Environment?.Project?.Name,
            "hostname" => DockerUtils.BuildContainerName(service.Environment?.Project?.Alias,
                service.Environment?.Alias, service.Alias, service.Name, service.Id),
            "environment_id" => service.EnvironmentId.ToString(),
            "project_id" => service.Environment?.ProjectId.ToString(),
            _ => null
        };
    }
}