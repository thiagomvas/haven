using Haven.Domain;

namespace Haven.Application.Common.Interfaces.Deployment;

/// <summary>
/// Everything a background cleanup job needs to tear down a service's deployment after the
/// service row itself has already been deleted (e.g. by a restore/sync), so the job can't
/// look it up by id.
/// </summary>
public sealed record ServiceCleanupInfo(
    Guid ServiceId,
    string ServiceName,
    string? ServiceAlias,
    ServiceType Type,
    string? SourceConfigJson);