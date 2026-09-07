using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.ServiceRegistry.Commands.AddDomain;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Sidecars.Domains;

public sealed class AddSidecarDomainEndpoint(IMediator mediator)
    : Endpoint<AddDomainCommand, ApiResponse<Guid>>
{
    public override void Configure()
    {
        Post("/sidecars/{sidecarId}/domains");

        Options(x => x.WithTags("Sidecars"));
        Summary(s =>
        {
            s.Summary = "Add a custom domain to a sidecar";
            s.Description = "Registers a custom hostname for a sidecar (currently only the Traefik sidecar's dashboard) in the service registry. Creates a registry entry for the sidecar if one doesn't exist yet.";
            s[201] = "Created";
            s[400] = "Validation error";
            s[404] = "Sidecar not found";
            s[409] = "A domain with that hostname already exists";
        });
    }

    public override async Task HandleAsync(AddDomainCommand req, CancellationToken ct)
    {
        req.SidecarId = Route<Guid>("sidecarId");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}