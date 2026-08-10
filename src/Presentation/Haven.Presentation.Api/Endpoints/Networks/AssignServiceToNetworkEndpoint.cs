using FastEndpoints;

using Haven.Application.Features.Networks.Commands.AssignServiceToNetwork;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Networks;

public sealed class AssignServiceToNetworkEndpoint(IMediator mediator) : Endpoint<AssignServiceToNetworkCommand>
{
    public override void Configure()
    {
        Post("/networks/{networkId}/services/{serviceId}");
        Options(x => x.WithTags("Networks"));
        Summary(s =>
        {
            s.Summary = "Assign a service to a network";
            s.Description = "Connects a running service's container to the network live, without a restart.";
            s[200] = "Assigned";
            s[404] = "Network or service not found";
        });
    }

    public override async Task HandleAsync(AssignServiceToNetworkCommand req, CancellationToken ct)
    {
        req.NetworkId = Route<Guid>("networkId");
        req.ServiceId = Route<Guid>("serviceId");
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
