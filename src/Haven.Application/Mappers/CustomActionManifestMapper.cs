using Haven.Application.Features.Services;
using Haven.Domain.Entities;
using Haven.Domain.ValueObjects;

namespace Haven.Application.Mappers;

public static class CustomActionManifestMapper
{
    public static CustomActionManifest ToManifest(this CustomAction action) => new()
    {
        Id = action.Id,
        ActionName = action.ActionName,
        Alias = action.Alias,
        ActionDescription = action.ActionDescription,
        Icon = action.Icon,
        Config = action.Config.ToManifest(),
        RequiredPermissions = [.. action.RequiredPermissions],
        Risk = action.Risk,
        Timeout = action.Timeout,
        Inputs = action.Inputs
            .Select(i => new CustomActionInputManifest
            {
                Name = i.Name,
                Label = i.Label,
                Description = i.Description,
                Required = i.Required,
                DefaultValue = i.DefaultValue
            })
            .ToList()
    };

    public static CustomAction ToEntity(this CustomActionManifest manifest, Guid serviceId)
    {
        return CustomAction.Reconstitute(
            manifest.Id == Guid.Empty ? Guid.CreateVersion7() : manifest.Id,
            serviceId,
            manifest.ActionName,
            manifest.Alias,
            manifest.ActionDescription,
            manifest.Icon,
            manifest.Config.ToDomain(),
            [.. manifest.RequiredPermissions],
            manifest.Risk,
            manifest.Timeout,
            [.. manifest.Inputs.Select(i => new CustomActionInput(i.Name, i.Label, i.Description, i.Required, i.DefaultValue))]);
    }

    public static ActionConfigManifest ToManifest(this ActionConfig config) => config switch
    {
        ExecActionConfig exec => new ActionConfigManifest
        {
            Type = "exec",
            Command = [.. exec.Command],
            WorkingDir = exec.WorkingDir,
            User = exec.User,
            Shell = exec.Shell
        },
        HttpActionConfig http => new ActionConfigManifest
        {
            Type = "http",
            Method = http.Method.Method,
            Url = http.Url,
            Headers = http.Headers.ToDictionary(h => h.Key, h => h.Value),
            Body = http.Body,
            SuccessStatusCodes = http.SuccessStatusCodes?.ToList()
        },
        _ => throw new InvalidOperationException($"Unknown action config type: {config.GetType().Name}")
    };

    public static ActionConfig ToDomain(this ActionConfigManifest manifest) => manifest.Type switch
    {
        "exec" => new ExecActionConfig(manifest.Command ?? [], manifest.WorkingDir, manifest.User, manifest.Shell),
        "http" => new HttpActionConfig(
            new HttpMethod(manifest.Method ?? "GET"),
            manifest.Url ?? string.Empty,
            manifest.Headers ?? new Dictionary<string, string>(),
            manifest.Body,
            manifest.SuccessStatusCodes),
        _ => throw new InvalidOperationException($"Unknown action config type: '{manifest.Type}'")
    };
}
