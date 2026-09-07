using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.ServiceRegistry.Commands.AttachDomainCertificate;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Sidecars.Domains;

public sealed class AttachSidecarDomainCertificateEndpoint(IMediator mediator)
    : Endpoint<AttachDomainCertificateCommand, ApiResponse<AttachDomainCertificateResult>>
{
    public override void Configure()
    {
        Post("/sidecars/{sidecarId}/domains/{domainId}/certificate");

        Options(x => x.WithTags("Sidecars"));
        Summary(s =>
        {
            s.Summary = "Attach a library TLS certificate to a sidecar's domain";
            s.Description = "Attaches a certificate from the SSL certificate library to a sidecar's domain (e.g. the Traefik dashboard) whose TLS mode is 'Custom'. Re-attaching replaces the previously attached certificate for this domain only.";
            s[200] = "Attached";
            s[400] = "Validation error";
            s[404] = "Domain or certificate not found";
        });
    }

    public override async Task HandleAsync(AttachDomainCertificateCommand req, CancellationToken ct)
    {
        req.DomainId = Route<Guid>("domainId");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}