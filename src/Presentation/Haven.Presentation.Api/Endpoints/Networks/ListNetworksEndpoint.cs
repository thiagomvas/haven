using FastEndpoints;

using Haven.Application.Common.Messaging;
using Haven.Application.Features.Networks.Queries.ListNetworks;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Networks;

public sealed class ListNetworksEndpoint(IMediator mediator)
    : Endpoint<ListNetworksQuery, List<NetworkDto>>
{
    public override void Configure()
    {
        Get("/networks");
        Options(x => x.WithTags("Networks"));
        Summary(s =>
        {
            s.Summary = "List networks";
            s.Description = "Returns all networks managed by Haven, optionally filtered by type.";
            s[200] = "OK";
        });
    }

    public override async Task HandleAsync(ListNetworksQuery req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
