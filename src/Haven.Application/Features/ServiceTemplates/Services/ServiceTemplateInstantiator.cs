using Haven.Application.Common;
using Haven.Application.Common.Templating;
using Haven.Application.Features.CustomActions;
using Haven.Application.Features.ServiceTemplates.Contracts;
using Haven.Application.Features.Services;
using Haven.Application.Mappers;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;

namespace Haven.Application.Features.ServiceTemplates.Services;

public sealed record ResolvedTemplateVariables(
    List<Haven.Domain.Entities.EnvironmentVariables> EnvironmentVariables,
    List<SecretVariable> Secrets);

/// <summary>
/// A file to write into a managed volume once it (and its owning service) have been created.
/// </summary>
public sealed record TemplateVolumeSeedFile(Guid VolumeId, string RelativePath, string Content);

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
            .. template.Container.Volumes.Select(v => v.Type == ServiceTemplateVolumeType.Managed
                ? ServiceVolume.Create(
                    serviceBase.Id,
                    VolumeType.Managed,
                    v.Name,
                    v.Mount,
                    backupEnabled: true)
                : ServiceVolume.Create(
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

    /// <summary>
    /// Resolves the seed files declared on the template's <see cref="ServiceTemplateVolumeType.Managed"/>
    /// volumes, matching them back to the <see cref="ServiceVolume"/>s just created by
    /// <see cref="ConfigureFromTemplate"/> via their container mount path. Must be called after
    /// <see cref="ConfigureFromTemplate"/>.
    /// </summary>
    public List<TemplateVolumeSeedFile> ResolveManagedVolumeSeedFiles(Service service, ServiceTemplate template, Dictionary<string, string> inputValues)
    {
        var seedFiles = new List<TemplateVolumeSeedFile>();

        foreach (var templateVolume in template.Container.Volumes)
        {
            if (templateVolume.Type != ServiceTemplateVolumeType.Managed || templateVolume.Files.Count == 0)
                continue;

            var volume = service.Volumes.First(v => v.Target == templateVolume.Mount);
            seedFiles.AddRange(templateVolume.Files.Select(file =>
                new TemplateVolumeSeedFile(volume.Id, file.Path, ResolveVariables(file.Content, inputValues))));
        }

        return seedFiles;
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
    /// <summary>
    /// Creates <see cref="CustomAction"/>s from the template's declared actions, resolving
    /// <c>inputs.*</c> placeholders now. Placeholders for unknown keys (action inputs) are kept for
    /// execution time. Secret template inputs cannot be referenced, as the resolved value would be
    /// stored in plaintext in the action config.
    /// </summary>
    public Result AddCustomActions(Service service, ServiceTemplate template, Dictionary<string, string> inputValues)
    {
        if (template.Actions.Count == 0)
            return Result.Success();

        var secretKeys = template.Inputs
            .Where(i => i.Type == TemplateInputFieldType.Secret)
            .Select(i => i.Key)
            .ToHashSet();
        var templateKeys = template.Inputs.Select(i => i.Key).ToHashSet();

        // Only keys that are real template inputs are resolved; everything else is an action input.
        string Resolve(string value) => TemplateExpressionResolver.Resolve(value, new Dictionary<string, TemplateNamespaceResolver>
        {
            ["inputs"] = key => templateKeys.Contains(key) ? inputValues.GetValueOrDefault(key) : null
        });

        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var action in template.Actions)
        {
            if (string.IsNullOrWhiteSpace(action.Name) || string.IsNullOrWhiteSpace(action.Alias))
                return Error.InvalidOperation("Template actions must declare a name and an alias.");

            if (!seenNames.Add(action.Name))
                return Error.InvalidOperation($"Template declares action '{action.Name}' more than once.");

            var referenced = ReferencedValues(action).SelectMany(v => TemplateExpressionResolver.FindKeys(v, "inputs"));
            if (referenced.Any(secretKeys.Contains))
                return Error.InvalidOperation($"Action '{action.Name}' cannot reference a secret input; reference it via the service environment instead.");

            ActionConfig config = action.Config.ToDomain() switch
            {
                ExecActionConfig exec => (ActionConfig)(exec with
                {
                    Command = exec.Command.Select(Resolve).ToList(),
                    WorkingDir = exec.WorkingDir is null ? null : Resolve(exec.WorkingDir),
                    User = exec.User is null ? null : Resolve(exec.User)
                }),
                HttpActionConfig http => (ActionConfig)(http with
                {
                    Url = Resolve(http.Url),
                    Headers = http.Headers.ToDictionary(h => h.Key, h => Resolve(h.Value)),
                    Body = http.Body is null ? null : Resolve(http.Body)
                }),
                var other => other
            };

            var validation = new ActionConfigValidator().Validate(config);
            if (!validation.IsValid)
                return Error.Validation($"Action '{action.Name}' is invalid: {validation.Errors[0].ErrorMessage}");

            if (action.TimeoutSeconds <= 0)
                return Error.Validation($"Action '{action.Name}' timeout must be greater than zero.");

            service.CustomActions.Add(CustomAction.Create(
                service.Id,
                action.Name,
                action.Alias,
                action.Description,
                action.Icon,
                config,
                [.. action.RequiredPermissions],
                action.Risk,
                TimeSpan.FromSeconds(action.TimeoutSeconds),
                [.. action.Inputs.Select(i => new CustomActionInput(
                    i.Name, i.Label, i.Description, i.Required, i.DefaultValue is null ? null : Resolve(i.DefaultValue)))]));
        }

        return Result.Success();
    }

    private static IEnumerable<string> ReferencedValues(ServiceTemplateAction action)
    {
        var c = action.Config;
        return (c.Command ?? [])
            .Concat((c.Headers ?? []).Values)
            .Concat(new[] { c.WorkingDir, c.User, c.Url, c.Body }.OfType<string>())
            .Concat(action.Inputs.Select(i => i.DefaultValue).OfType<string>());
    }
}
