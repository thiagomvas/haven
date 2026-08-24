using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.ServiceRegistry.Commands.UploadDomainCertificate;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.ServiceRegistry.Domains;

public sealed class UploadDomainCertificateEndpoint(IMediator mediator)
    : Endpoint<UploadDomainCertificateCommand, ApiResponse<UploadDomainCertificateResult>>
{
    public override void Configure()
    {
        Post("/service-registry/{serviceId}/domains/{domainId}/certificate");

        Options(x => x.WithTags("ServiceRegistry"));
        Summary(s =>
        {
            s.Summary = "Upload a custom TLS certificate for a domain";
            s.Description = "Attaches a bring-your-own certificate/key pair to a domain whose TLS mode is 'Custom'. Re-uploading rotates the existing certificate in place.";
            s[200] = "Uploaded";
            s[400] = "Validation error";
            s[404] = "Service registry entry or domain not found";
        });
    }

    public override async Task HandleAsync(UploadDomainCertificateCommand req, CancellationToken ct)
    {
        req.ServiceId = Route<Guid>("serviceId");
        req.DomainId = Route<Guid>("domainId");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
