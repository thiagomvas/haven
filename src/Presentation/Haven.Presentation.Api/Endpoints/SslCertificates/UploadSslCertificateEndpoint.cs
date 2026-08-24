using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.SslCertificates.Commands.UploadSslCertificate;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.SslCertificates;

public sealed class UploadSslCertificateEndpoint(IMediator mediator)
    : Endpoint<UploadSslCertificateCommand, ApiResponse<UploadSslCertificateResult>>
{
    public override void Configure()
    {
        Post("/ssl-certificates");

        Options(x => x.WithTags("SslCertificates"));
        Summary(s =>
        {
            s.Summary = "Upload an SSL certificate to the library";
            s.Description = "Adds a bring-your-own certificate/key pair to the SSL certificate library, so it can be attached to any number of domains without re-uploading (useful for wildcard certificates).";
            s[200] = "Uploaded";
            s[400] = "Validation error";
        });
    }

    public override async Task HandleAsync(UploadSslCertificateCommand req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
