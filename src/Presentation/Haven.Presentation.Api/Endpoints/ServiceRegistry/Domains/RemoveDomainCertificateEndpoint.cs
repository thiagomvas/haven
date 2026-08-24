using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.ServiceRegistry.Commands.RemoveDomainCertificate;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.ServiceRegistry.Domains;

public sealed class RemoveDomainCertificateEndpoint(IMediator mediator)
    : Endpoint<RemoveDomainCertificateCommand, ApiResponse>
{
    public override void Configure()
    {
        Delete("/service-registry/{serviceId}/domains/{domainId}/certificate");

        Options(x => x.WithTags("ServiceRegistry"));
        Summary(s =>
        {
            s.Summary = "Remove a domain's custom TLS certificate";
            s.Description = "Removes an uploaded certificate/key pair from a domain. Does not change the domain's TLS mode - a 'Custom' mode domain with no certificate is left as a flagged, incomplete state.";
            s[200] = "Removed";
            s[404] = "Service registry entry or domain not found";
        });
    }

    public override async Task HandleAsync(RemoveDomainCertificateCommand req, CancellationToken ct)
    {
        req.ServiceId = Route<Guid>("serviceId");
        req.DomainId = Route<Guid>("domainId");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
