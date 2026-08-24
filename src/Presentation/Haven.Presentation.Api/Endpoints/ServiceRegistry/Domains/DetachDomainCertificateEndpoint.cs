using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.ServiceRegistry.Commands.DetachDomainCertificate;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.ServiceRegistry.Domains;

public sealed class DetachDomainCertificateEndpoint(IMediator mediator)
    : Endpoint<DetachDomainCertificateCommand, ApiResponse>
{
    public override void Configure()
    {
        Delete("/service-registry/{serviceId}/domains/{domainId}/certificate");

        Options(x => x.WithTags("ServiceRegistry"));
        Summary(s =>
        {
            s.Summary = "Detach a domain's TLS certificate";
            s.Description = "Detaches the currently-attached library certificate from a domain. Does not change the domain's TLS mode - a 'Custom' mode domain with no certificate is left as a flagged, incomplete state. The library certificate itself is untouched.";
            s[200] = "Detached";
            s[404] = "Service registry entry or domain not found";
        });
    }

    public override async Task HandleAsync(DetachDomainCertificateCommand req, CancellationToken ct)
    {
        req.ServiceId = Route<Guid>("serviceId");
        req.DomainId = Route<Guid>("domainId");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
