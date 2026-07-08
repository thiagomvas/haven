using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Services.Queries.ListVolumesForService;

[RequirePermission(Permissions.ProjectManagement.Read)]
public sealed class ListVolumesForServiceQuery : IQuery<IReadOnlyList<ServiceVolumeDto>>
{
    public Guid ServiceId { get; set; }
}