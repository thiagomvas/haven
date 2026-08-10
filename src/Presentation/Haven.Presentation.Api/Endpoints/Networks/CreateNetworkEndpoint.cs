using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Networks.Commands.CreateNetwork;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Networks;

public sealed class CreateNetworkEndpoint(IMediator mediator) : Endpoint<CreateNetworkCommand, ApiResponse<Guid>>
{
    public override void Configure()
    {
        Post("/networks");
        Options(x => x.WithTags("Networks"));
        Summary(s =>
        {
            s.Summary = "Create a network";
            s.Description = "Creates a shared Docker network that services can be assigned to.";
            s[201] = "Created";
            s[400] = "Validation error";
        });
    }

    public override async Task HandleAsync(CreateNetworkCommand req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
