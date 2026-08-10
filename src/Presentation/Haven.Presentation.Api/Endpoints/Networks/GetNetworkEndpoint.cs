using FastEndpoints;

using Haven.Application.Features.Networks.Queries.GetNetwork;
using Haven.Application.Features.Networks.Queries.ListNetworks;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Networks;

public sealed class GetNetworkEndpoint(IMediator mediator) : Endpoint<GetNetworkQuery, NetworkDto>
{
    public override void Configure()
    {
        Get("/networks/{networkId}");
        Options(x => x.WithTags("Networks"));
        Summary(s =>
        {
            s.Summary = "Get a network";
            s.Description = "Returns a single network by ID, including its assigned services.";
            s[200] = "OK";
            s[404] = "Network not found";
        });
    }

    public override async Task HandleAsync(GetNetworkQuery req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
