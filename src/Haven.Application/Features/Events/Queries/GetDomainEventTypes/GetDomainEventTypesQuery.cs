using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Events.Queries.GetDomainEventTypes;

public sealed class GetDomainEventTypesQuery : IQuery<DomainEventTypeDto[]>;