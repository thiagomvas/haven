using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Services.Queries.GetVolumeFiles;

[RequirePermission(Permissions.ProjectManagement.Read)]
public sealed class GetVolumeFilesQuery : IQuery<IReadOnlyList<ManagedVolumeFileEntry>>
{
    public Guid ServiceId { get; set; }
    public Guid VolumeId { get; set; }
}