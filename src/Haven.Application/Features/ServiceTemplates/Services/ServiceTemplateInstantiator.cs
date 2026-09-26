using System.Text.RegularExpressions;

using Haven.Application.Features.ServiceTemplates.Contracts;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;

namespace Haven.Application.Features.ServiceTemplates.Services;

public sealed record ResolvedTemplateVariables(
    List<Haven.Domain.Entities.EnvironmentVariables> EnvironmentVariables,
    List<SecretVariable> Secrets);

public partial class ServiceTemplateInstantiator
{
    [GeneratedRegex(@"\$\{\{\s*inputs\.(.+?)\s*\}\}", RegexOptions.Compiled)]
    private static partial Regex VariablePattern();

    public Service ConfigureFromTemplate(Service serviceBase, ServiceTemplate template, Dictionary<string, string> inputValues)
    {
        serviceBase.Type = ServiceType.DockerImage;
        serviceBase.SourceConfig = new DockerConfig()
        {
            Image = ResolveVariables(template.Container.DockerImage, inputValues),
            CommandArgs = template.Container.CommandArgs
                .Select(arg => ResolveVariables(arg, inputValues))
                .ToList()
        };

        serviceBase.Volumes =
        [
            .. template.Container.Volumes.Select(v => ServiceVolume.Create(
                serviceBase.Id,
                VolumeType.Named,
                $"haven-{serviceBase.Alias}-{serviceBase.Id.ToString("N")[..8]}-{v.Name}",
                v.Mount,
                $"haven-{serviceBase.Alias}-{serviceBase.Id.ToString("N")[..8]}-{v.Name}",
                false,
                true))
        ];
        return serviceBase;
    }

    public ResolvedTemplateVariables ResolveEnvironmentVariables(Service serviceBase, ServiceTemplate template, Dictionary<string, string> inputValues)
    {
        var secretKeys = template.Inputs
            .Where(i => i.Type == TemplateInputFieldType.Secret)
            .Select(i => i.Key)
            .ToHashSet();

        var environmentVariables = new List<Haven.Domain.Entities.EnvironmentVariables>();
        var secrets = new List<SecretVariable>();

        foreach (var (key, value) in template.Container.Env)
        {
            var referencesSecretInput = VariablePattern().Matches(value).Any(m => secretKeys.Contains(m.Groups[1].Value));
            var resolvedValue = ResolveVariables(value, inputValues);

            if (referencesSecretInput)
            {
                secrets.Add(new SecretVariable
                {
                    ParentId = serviceBase.Id,
                    ParentType = EnvironmentVariableParentType.Service,
                    Key = key,
                    Value = EncryptedValue.From(resolvedValue)
                });
            }
            else
            {
                environmentVariables.Add(new Haven.Domain.Entities.EnvironmentVariables
                {
                    ParentId = serviceBase.Id,
                    ParentType = EnvironmentVariableParentType.Service,
                    Key = key,
                    Value = resolvedValue
                });
            }
        }

        return new ResolvedTemplateVariables(environmentVariables, secrets);
    }

    public string ResolveVariables(string input, Dictionary<string, string> inputValues)
    {
        return VariablePattern().Replace(input, match =>
        {
            var key = match.Groups[1].Value;
            return inputValues.TryGetValue(key, out var value) ? value : match.Value;
        });
    }
}
