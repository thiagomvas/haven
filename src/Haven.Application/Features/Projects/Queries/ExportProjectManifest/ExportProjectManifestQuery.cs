using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Projects.Queries.ExportProjectManifest;

// Manifests include service configuration (feature flags, source config), so ManageConfig is required alongside Read.
[RequirePermission(Permissions.ProjectManagement.Read)]
[RequirePermission(Permissions.ProjectManagement.ManageConfig)]
public sealed class ExportProjectManifestQuery : IQuery<string>
{
    public Guid ProjectId { get; init; }

    /// <summary>Include the project's environments in the manifest.</summary>
    public bool IncludeEnvironments { get; init; }

    /// <summary>Include each environment's services. Requires <see cref="IncludeEnvironments"/>.</summary>
    public bool IncludeServices { get; init; }
}
