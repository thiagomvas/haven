using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Services.Queries.GetService;

[RequirePermission(Permissions.Services.View)]
public sealed class GetServiceQuery : IQuery<ServiceDto>
{
    public Guid ProjectId { get; init; }
    public Guid EnvironmentId { get; init; }
    public Guid ServiceId { get; init; }
}
