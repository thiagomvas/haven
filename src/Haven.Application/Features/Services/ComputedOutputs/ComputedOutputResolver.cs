using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Templating;
using Haven.Domain.Aggregates;

namespace Haven.Application.Features.Services.ComputedOutputs;

public sealed class ComputedOutputResolver(
    IEnvironmentVariableService environmentVariableService,
    ISecretVariableService secretVariableService) : IComputedOutputResolver
{
    private const string MaskedPlaceholder = "••••••••";

    public async Task<IReadOnlyList<ResolvedComputedOutput>> ResolveAsync(
        Service service,
        ServiceRegistryEntry? registry,
        CancellationToken cancellationToken)
    {
        if (service.ComputedProperties.Count == 0)
            return [];

        var plainVars = await environmentVariableService.BuildVariablesForServiceAsync(service.Id, cancellationToken);
        var secretVars = await secretVariableService.GetSecretsAsEnvironmentVariablesForServiceAsync(service.Id, cancellationToken);

        var envMap = plainVars.ToDictionary(v => v.Key, v => v.Value);
        var secretKeys = new HashSet<string>();
        foreach (var secret in secretVars)
        {
            envMap[secret.Key] = secret.Value;
            secretKeys.Add(secret.Key);
        }

        var results = new List<ResolvedComputedOutput>();

        foreach (var property in service.ComputedProperties)
        {
            results.Add(ResolveOne(property, envMap, secretKeys, registry));
        }

        return results;
    }

    private static ResolvedComputedOutput ResolveOne(
        Domain.Entities.ServiceComputedProperty property,
        Dictionary<string, string?> envMap,
        HashSet<string> secretKeys,
        ServiceRegistryEntry? registry)
    {
        var referencedEnvKeys = TemplateExpressionResolver.FindKeys(property.Template, "env");
        var missingKey = referencedEnvKeys.FirstOrDefault(k => !envMap.TryGetValue(k, out var v) || string.IsNullOrEmpty(v));
        if (missingKey is not null)
            return Unavailable(property, $"MissingVariable:{missingKey}");

        var referencesHost = TemplateExpressionResolver.FindKeys(property.Template, "runtime").Contains("host");
        if (referencesHost && string.IsNullOrEmpty(registry?.IpAddress))
            return Unavailable(property, "NotRunning");

        var usedSecretValues = new List<string>();
        var value = TemplateExpressionResolver.Resolve(property.Template, new Dictionary<string, TemplateNamespaceResolver>
        {
            ["env"] = key =>
            {
                if (!envMap.TryGetValue(key, out var v) || v is null)
                    return null;
                if (secretKeys.Contains(key))
                    usedSecretValues.Add(v);
                return v;
            },
            ["runtime"] = key => key == "host" ? registry?.IpAddress : null
        });

        string? maskedPreview = null;
        if (property.IsSecret)
        {
            maskedPreview = value;
            foreach (var secretValue in usedSecretValues.Distinct().OrderByDescending(s => s.Length))
            {
                if (string.IsNullOrEmpty(secretValue)) continue;
                maskedPreview = maskedPreview!.Replace(Uri.EscapeDataString(secretValue), MaskedPlaceholder)
                    .Replace(secretValue, MaskedPlaceholder);
            }
        }

        return new ResolvedComputedOutput(
            property.Key,
            property.Label,
            property.IsSecret,
            IsAvailable: true,
            UnavailableReason: null,
            Value: value,
            MaskedPreview: property.IsSecret ? maskedPreview : value);
    }

    private static ResolvedComputedOutput Unavailable(Domain.Entities.ServiceComputedProperty property, string reason) =>
        new(property.Key, property.Label, property.IsSecret, IsAvailable: false, UnavailableReason: reason, Value: null, MaskedPreview: null);
}
