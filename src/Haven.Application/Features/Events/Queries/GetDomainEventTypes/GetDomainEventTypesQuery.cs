using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Events.Queries.GetDomainEventTypes;

[AuthenticatedOnly]
public sealed class GetDomainEventTypesQuery : IQuery<DomainEventTypeDto[]>;