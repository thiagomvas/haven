using Haven.Application.Common.Templating;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;

namespace Haven.Infrastructure.Extensions;

public static class TemplatingExtensions
{
    public static Dictionary<string, TemplateNamespaceResolver> ResolveTemplateNamespaces(this Service service, List<EnvironmentVariables>? environmentVariables = null)
        => service.ResolveTemplateNamespaces([], environmentVariables ?? []);

    public static Dictionary<string, TemplateNamespaceResolver> ResolveTemplateNamespaces(this Service service,
        Dictionary<string, string> inputValues, List<EnvironmentVariables> environmentVariables,
        List<EnvironmentVariables>? secrets = null)
    {
        return new Dictionary<string, TemplateNamespaceResolver>()
        {
            [TemplateNamespaces.Inputs] = key => inputValues.TryGetValue(key, out var value) ? value : null,
            [TemplateNamespaces.Runtime] = key => service.ResolveRuntimeVariable(key),
            [TemplateNamespaces.EnvVariables] = key => environmentVariables.FirstOrDefault(e => e.Key == key)?.Value,
            [TemplateNamespaces.Secrets] = key => secrets?.FirstOrDefault(e => e.Key == key)?.Value
        };
    }

    private static string? ResolveRuntimeVariable(this Service service, string key)
    {
        return key switch
        {
            "id" => service.Id.ToString(),
            "alias" => service.Alias,
            _ => null
        };
    }
}