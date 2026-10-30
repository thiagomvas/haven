using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Environments.Queries.ExportEnvironmentManifest;

// Manifests include service configuration (feature flags, source config), so ManageConfig is required alongside Read.
[RequirePermission(Permissions.ProjectManagement.Read)]
[RequirePermission(Permissions.ProjectManagement.ManageConfig)]
public sealed class ExportEnvironmentManifestQuery : IQuery<string>
{
    public Guid EnvironmentId { get; init; }

    /// <summary>Include the environment's services in the manifest.</summary>
    public bool IncludeServices { get; init; }
}
