using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.ServiceRegistry.Commands.DeleteDomain;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Sidecars.Domains;

public sealed class DeleteSidecarDomainEndpoint(IMediator mediator)
    : Endpoint<DeleteDomainCommand, ApiResponse>
{
    public override void Configure()
    {
        Delete("/sidecars/{sidecarId}/domains/{domainId}");

        Options(x => x.WithTags("Sidecars"));
        Summary(s =>
        {
            s.Summary = "Delete a sidecar's custom domain";
            s.Description = "Removes a registered domain from a sidecar's service registry entry.";
            s[200] = "Deleted";
            s[404] = "Service registry entry or domain not found";
        });
    }

    public override async Task HandleAsync(DeleteDomainCommand req, CancellationToken ct)
    {
        req.SidecarId = Route<Guid>("sidecarId");
        req.DomainId = Route<Guid>("domainId");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}