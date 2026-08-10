using FastEndpoints;

using Haven.Application.Features.Networks.Commands.DeleteNetwork;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Networks;

public sealed class DeleteNetworkEndpoint(IMediator mediator) : Endpoint<DeleteNetworkCommand>
{
    public override void Configure()
    {
        Delete("/networks/{networkId}");
        Options(x => x.WithTags("Networks"));
        Summary(s =>
        {
            s.Summary = "Delete a network";
            s.Description = "Disconnects all assigned services and permanently deletes a shared network.";
            s[200] = "Deleted";
            s[404] = "Network not found";
        });
    }

    public override async Task HandleAsync(DeleteNetworkCommand req, CancellationToken ct)
    {
        req.NetworkId = Route<Guid>("networkId");
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}