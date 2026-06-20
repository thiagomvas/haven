using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.Entities;
using Haven.Infrastructure.Persistence.Extensions;

namespace Haven.Infrastructure.Persistence.Repositories;

public sealed class EventRepository(HavenDbContext context) : IEventRepository
{
    public Task<PagedResult<Event>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? eventType,
        DateTime? from,
        DateTime? to,
        bool ascending,
        CancellationToken cancellationToken)
    {
        var query = context.Events.AsQueryable();

        if (eventType is not null)
            query = query.Where(e => e.EventType == eventType);

        if (from is not null)
            query = query.Where(e => e.TriggeredAt >= from);

        if (to is not null)
            query = query.Where(e => e.TriggeredAt <= to);

        query = ascending
            ? query.OrderBy(e => e.TriggeredAt)
            : query.OrderByDescending(e => e.TriggeredAt);

        return query.ToPagedResultAsync(pageNumber, pageSize, cancellationToken);
    }
}