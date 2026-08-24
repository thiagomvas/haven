using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.ServiceRegistry.Commands.UpdateDomain;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Sidecars.Domains;

public sealed class UpdateSidecarDomainEndpoint(IMediator mediator)
    : Endpoint<UpdateDomainCommand, ApiResponse>
{
    public override void Configure()
    {
        Patch("/sidecars/{sidecarId}/domains/{domainId}");

        Options(x => x.WithTags("Sidecars"));
        Summary(s =>
        {
            s.Summary = "Update a sidecar's custom domain";
            s.Description = "Partially updates a sidecar's registered domain (hostname, container port and/or TLS mode).";
            s[200] = "Updated";
            s[400] = "Validation error";
            s[404] = "Service registry entry or domain not found";
            s[409] = "A domain with that hostname already exists";
        });
    }

    public override async Task HandleAsync(UpdateDomainCommand req, CancellationToken ct)
    {
        req.SidecarId = Route<Guid>("sidecarId");
        req.DomainId = Route<Guid>("domainId");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
