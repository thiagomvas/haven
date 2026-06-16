using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Events.Queries.GetDomainEventTypes;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Events;

public sealed class GetDomainEventTypesEndpoint(IMediator mediator) : EndpointWithoutRequest<ApiResponse<DomainEventTypeDto[]>>
{
    public override void Configure()
    {
        Get("/events/types");
        AllowAnonymous();
        Options(x => x.WithTags("Events"));
        Summary(s =>
        {
            s.Summary = "Get domain event types";
            s.Description = "Returns all registered domain event types with their names and i18n keys.";
            s[200] = "OK";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await mediator.Send(new GetDomainEventTypesQuery(), ct);
        await this.SendResultAsync(result, ct);
    }
}