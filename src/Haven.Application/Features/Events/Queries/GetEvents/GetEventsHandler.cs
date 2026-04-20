using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Events.Queries.GetEvents;

public sealed class GetEventsHandler(IEventRepository repository)
    : IPagedQueryHandler<GetEventsQuery, EventDto>
{
    public async ValueTask<Result<PagedResult<EventDto>>> Handle(GetEventsQuery query, CancellationToken cancellationToken)
    {
        var paged = await repository.GetPagedAsync(
            query.PageNumber,
            query.PageSize,
            query.EventType,
            query.From,
            query.To,
            query.Ascending,
            cancellationToken);

        var items = paged.Items
            .Select(e => new EventDto(e.Id, e.EventType, e.Message, e.Payload, e.TriggeredAt))
            .ToList();

        return Result<PagedResult<EventDto>>.Success(
            new PagedResult<EventDto>(items, paged.TotalCount, paged.PageNumber, paged.PageSize));
    }
}
