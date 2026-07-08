using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Services.Queries.GetVolumeFileContent;

[RequirePermission(Permissions.ProjectManagement.Read)]
public sealed class GetVolumeFileContentQuery : IQuery<string>
{
    public Guid ServiceId { get; set; }
    public Guid VolumeId { get; set; }
    public string Path { get; set; } = string.Empty;
}
