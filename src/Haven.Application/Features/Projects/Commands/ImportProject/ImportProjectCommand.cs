using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Projects.Commands.ImportProject;

[RequirePermission(Permissions.ProjectManagement.Create)]
public sealed class ImportProjectCommand : ICommand<Guid>, IMutatesManifestState
{
    /// <summary>Project manifest YAML, as produced by the project manifest export.</summary>
    public string Yaml { get; set; } = string.Empty;

    /// <summary>Overrides the manifest's project name when set.</summary>
    public string? Name { get; set; }

    /// <summary>Overrides the manifest's project alias when set.</summary>
    public string? Alias { get; set; }

    /// <summary>Import the environments found in the manifest.</summary>
    public bool IncludeEnvironments { get; set; }

    /// <summary>Import the services of each imported environment. Requires <see cref="IncludeEnvironments"/>.</summary>
    public bool IncludeServices { get; set; }
}
