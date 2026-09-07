using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.ServiceRegistry.Queries.GetDomainCertificateStatus;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.ServiceRegistry.Domains;

public sealed class GetDomainCertificateStatusEndpoint(IMediator mediator)
    : Endpoint<GetDomainCertificateStatusQuery, ApiResponse<DomainCertificateStatusDto>>
{
    public override void Configure()
    {
        Get("/domains/{domainId}/certificate/status");

        Options(x => x.WithTags("ServiceRegistry"));
        Summary(s =>
        {
            s.Summary = "Get a domain's certificate status";
            s.Description = "Fetches on-demand TLS certificate status for a domain - read from Haven's own DB for 'Custom' mode, or Traefik's live REST API for 'Acme' mode. Not polled/cached.";
            s[200] = "OK";
            s[404] = "Service registry entry or domain not found";
        });
    }

    public override async Task HandleAsync(GetDomainCertificateStatusQuery req, CancellationToken ct)
    {
        req.DomainId = Route<Guid>("domainId");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}