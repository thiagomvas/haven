using Haven.Application.Common;
using Haven.Application.Common.Templating;
using Haven.Application.Features.ServiceTemplates.Contracts;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;

namespace Haven.Application.Features.ServiceTemplates.Services;

public sealed record ResolvedTemplateVariables(
    List<Haven.Domain.Entities.EnvironmentVariables> EnvironmentVariables,
    List<SecretVariable> Secrets);

public sealed class ServiceTemplateInstantiator
{
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
            var referencesSecretInput = TemplateExpressionResolver.FindKeys(value, "inputs").Any(secretKeys.Contains);
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
        return TemplateExpressionResolver.Resolve(input, new Dictionary<string, TemplateNamespaceResolver>
        {
            ["inputs"] = key => inputValues.GetValueOrDefault(key)
        });
    }

    /// <summary>
    /// Creates <see cref="ServiceComputedProperty"/> records from the template's declared outputs,
    /// resolving <c>inputs.*</c> and <c>container.port</c> now. References to <c>env.*</c> and
    /// <c>runtime.*</c> are intentionally left unresolved, so no secret value is ever persisted —
    /// they're resolved at read time instead (see <c>IComputedOutputResolver</c>).
    /// </summary>
    public Result AddComputedProperties(Service service, ServiceTemplate template, Dictionary<string, string> inputValues)
    {
        if (template.Outputs.Count == 0)
            return Result.Success();

        var secretKeys = template.Inputs
            .Where(i => i.Type == TemplateInputFieldType.Secret)
            .Select(i => i.Key)
            .ToHashSet();

        foreach (var output in template.Outputs)
        {
            if (TemplateExpressionResolver.FindKeys(output.Value, "inputs").Any(secretKeys.Contains))
                return Error.InvalidOperation($"Output '{output.Key}' cannot reference a secret input directly; reference it via 'env.<KEY>' instead.");

            if (TemplateExpressionResolver.FindKeys(output.Value, "container").Contains("port") && template.Container.Port is null)
                return Error.InvalidOperation($"Output '{output.Key}' references 'container.port', but the template does not declare a port.");

            var partiallyResolved = TemplateExpressionResolver.Resolve(output.Value, new Dictionary<string, TemplateNamespaceResolver>
            {
                ["inputs"] = key => inputValues.GetValueOrDefault(key),
                ["container"] = key => key == "port" ? template.Container.Port?.ToString() : null
            });

            service.AddComputedProperty(output.Key, output.Label, partiallyResolved, output.Secret);
        }

        return Result.Success();
    }
}
