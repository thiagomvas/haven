using FastEndpoints;

using Haven.Application.Features.Networks.Queries.SearchAttachableServices;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Networks;

public sealed class SearchAttachableServicesEndpoint(IMediator mediator)
    : Endpoint<SearchAttachableServicesQuery, List<AttachableServiceDto>>
{
    public override void Configure()
    {
        Get("/networks/{networkId}/attachable-services");
        Options(x => x.WithTags("Networks"));
        Summary(s =>
        {
            s.Summary = "Search services that can be attached to a network";
            s.Description = "Searches services by name, environment or project, excluding services already attached to this network.";
            s[200] = "OK";
            s[404] = "Network not found";
        });
    }

    public override async Task HandleAsync(SearchAttachableServicesQuery req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
