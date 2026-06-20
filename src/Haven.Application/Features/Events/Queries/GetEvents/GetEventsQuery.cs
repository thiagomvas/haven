using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Events.Queries.GetEvents;

[RequirePermission(Permissions.ProjectManagement.Read)]
public sealed class GetEventsQuery : PagedQuery<EventDto>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? EventType { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public bool Ascending { get; init; } = false;
}