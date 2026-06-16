using Haven.Application.Common;
using Haven.Application.Common.Messaging;

using Haven.Domain.Events;

namespace Haven.Application.Features.Events.Queries.GetDomainEventTypes;

public sealed class GetDomainEventTypesHandler : IQueryHandler<GetDomainEventTypesQuery, DomainEventTypeDto[]>
{
    public ValueTask<Result<DomainEventTypeDto[]>> Handle(GetDomainEventTypesQuery query, CancellationToken cancellationToken)
    {
        var types = DomainEvent.AllEventTypes
            .Select(t => new DomainEventTypeDto(t.Name, DomainEvent.GetI18NKey(t)))
            .ToArray();

        return ValueTask.FromResult(Result<DomainEventTypeDto[]>.Success(types));
    }
}