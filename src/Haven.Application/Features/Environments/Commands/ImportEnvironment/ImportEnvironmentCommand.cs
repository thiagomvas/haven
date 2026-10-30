using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Environments.Commands.ImportEnvironment;

[RequirePermission(Permissions.ProjectManagement.Create)]
public sealed class ImportEnvironmentCommand : ICommand<Guid>, IMutatesManifestState
{
    public Guid ProjectId { get; set; }

    /// <summary>Environment manifest YAML, as produced by the environment manifest export.</summary>
    public string Yaml { get; set; } = string.Empty;

    /// <summary>Overrides the manifest's environment name when set.</summary>
    public string? Name { get; set; }

    /// <summary>Overrides the manifest's environment alias when set. Required if the manifest has none.</summary>
    public string? Alias { get; set; }

    /// <summary>Import the services found in the manifest.</summary>
    public bool IncludeServices { get; set; }
}
