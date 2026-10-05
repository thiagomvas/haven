using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.CustomActions.Queries.ListCustomActionsForService;

[RequirePermission(Permissions.ProjectManagement.Read)]
public sealed class ListCustomActionsForServiceQuery : IQuery<IReadOnlyList<CustomActionDto>>
{
    public Guid ServiceId { get; set; }
}