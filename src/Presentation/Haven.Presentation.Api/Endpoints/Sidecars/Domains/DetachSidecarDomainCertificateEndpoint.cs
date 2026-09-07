using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.ServiceRegistry.Commands.DetachDomainCertificate;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Sidecars.Domains;

public sealed class DetachSidecarDomainCertificateEndpoint(IMediator mediator)
    : Endpoint<DetachDomainCertificateCommand, ApiResponse>
{
    public override void Configure()
    {
        Delete("/sidecars/{sidecarId}/domains/{domainId}/certificate");

        Options(x => x.WithTags("Sidecars"));
        Summary(s =>
        {
            s.Summary = "Detach a sidecar domain's TLS certificate";
            s.Description = "Detaches the currently-attached library certificate from a sidecar's domain (e.g. the Traefik dashboard). Does not change the domain's TLS mode. The library certificate itself is untouched.";
            s[200] = "Detached";
            s[404] = "Domain not found";
        });
    }

    public override async Task HandleAsync(DetachDomainCertificateCommand req, CancellationToken ct)
    {
        req.DomainId = Route<Guid>("domainId");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}