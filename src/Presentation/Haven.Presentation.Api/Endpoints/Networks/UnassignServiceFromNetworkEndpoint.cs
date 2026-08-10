using FastEndpoints;

using Haven.Application.Features.Networks.Commands.UnassignServiceFromNetwork;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Networks;

public sealed class UnassignServiceFromNetworkEndpoint(IMediator mediator) : Endpoint<UnassignServiceFromNetworkCommand>
{
    public override void Configure()
    {
        Delete("/networks/{networkId}/services/{serviceId}");
        Options(x => x.WithTags("Networks"));
        Summary(s =>
        {
            s.Summary = "Unassign a service from a network";
            s.Description = "Disconnects a running service's container from the network live, without a restart.";
            s[200] = "Unassigned";
            s[404] = "Network or service not found";
        });
    }

    public override async Task HandleAsync(UnassignServiceFromNetworkCommand req, CancellationToken ct)
    {
        req.NetworkId = Route<Guid>("networkId");
        req.ServiceId = Route<Guid>("serviceId");
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}