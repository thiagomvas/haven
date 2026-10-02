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
        List<EnvironmentVariables>? secrets = null)
    {
        return new Dictionary<string, TemplateNamespaceResolver>()
        {
            [TemplateNamespaces.Inputs] = inputValues.GetValueOrDefault,
            [TemplateNamespaces.Runtime] = service.ResolveRuntimeVariable,
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