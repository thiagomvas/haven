using FastEndpoints;

using Haven.Application.Common.Messaging;
using Haven.Application.Features.Events.Queries.GetEvents;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Events;

public sealed class GetEventsEndpoint(IMediator mediator)
    : Endpoint<GetEventsQuery, PagedResult<EventDto>>
{
    public override void Configure()
    {
        Get("/events");
        
        Options(x => x.WithTags("Events"));
        Summary(s =>
        {
            s.Summary = "List events";
            s.Description = "Returns a paginated list of domain events with optional filtering by type and date range.";
            s[200] = "OK";
        });
    }

    public override async Task HandleAsync(GetEventsQuery req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}