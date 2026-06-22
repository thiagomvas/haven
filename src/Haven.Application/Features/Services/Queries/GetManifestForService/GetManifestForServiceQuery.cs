using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Services.Queries.GetManifestForService;

[RequirePermission(Permissions.ProjectManagement.Read)]
public class GetManifestForServiceQuery : IQuery<string>
{
    public Guid ServiceId { get; set; }
}