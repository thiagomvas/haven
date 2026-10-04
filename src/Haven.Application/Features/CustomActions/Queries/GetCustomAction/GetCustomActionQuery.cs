using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.CustomActions.Queries.GetCustomAction;

[RequirePermission(Permissions.ProjectManagement.Read)]
public sealed class GetCustomActionQuery : IQuery<CustomActionDto>
{
    public Guid ServiceId { get; set; }
    public Guid ActionId { get; set; }
}
