using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Services.Queries.GetServiceDependants;

[RequirePermission(Permissions.ProjectManagement.Read)]
public sealed class GetServiceDependantsQuery : IQuery<IReadOnlyList<ServiceDependantDto>>
{
    public Guid ProjectId { get; init; }
    public Guid EnvironmentId { get; init; }
    public Guid ServiceId { get; init; }
}

public sealed record ServiceDependantDto(Guid Id, string Name, string? Alias);
