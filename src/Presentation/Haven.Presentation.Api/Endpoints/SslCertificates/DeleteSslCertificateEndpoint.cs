using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.SslCertificates.Commands.DeleteSslCertificate;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.SslCertificates;

public sealed class DeleteSslCertificateEndpoint(IMediator mediator)
    : Endpoint<DeleteSslCertificateCommand, ApiResponse>
{
    public override void Configure()
    {
        Delete("/ssl-certificates/{certificateId}");

        Options(x => x.WithTags("SslCertificates"));
        Summary(s =>
        {
            s.Summary = "Delete an SSL certificate";
            s.Description = "Deletes a certificate from the library, detaching it from every domain it's currently attached to. Those domains fall back to 'Custom mode, no certificate attached' rather than being deleted themselves.";
            s[200] = "Deleted";
            s[404] = "Certificate not found";
        });
    }

    public override async Task HandleAsync(DeleteSslCertificateCommand req, CancellationToken ct)
    {
        req.CertificateId = Route<Guid>("certificateId");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}