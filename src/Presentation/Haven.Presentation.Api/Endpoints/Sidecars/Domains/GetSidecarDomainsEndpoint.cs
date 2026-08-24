using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.ServiceRegistry.Queries.GetServiceRegistryEntries;
using Haven.Application.Features.Sidecars.Queries.GetSidecarDomains;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Sidecars.Domains;

public sealed class GetSidecarDomainsEndpoint(IMediator mediator)
    : Endpoint<GetSidecarDomainsQuery, ApiResponse<List<ServiceRegistryDomainDto>>>
{
    public override void Configure()
    {
        Get("/sidecars/{sidecarId}/domains");

        Options(x => x.WithTags("Sidecars"));
        Summary(s =>
        {
            s.Summary = "List a sidecar's registered domains";
            s.Description = "Returns the domains registered for a sidecar (currently only the Traefik sidecar's dashboard), or an empty list if it has never been registered.";
            s[200] = "OK";
        });
    }

    public override async Task HandleAsync(GetSidecarDomainsQuery req, CancellationToken ct)
    {
        req.SidecarId = Route<Guid>("sidecarId");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
