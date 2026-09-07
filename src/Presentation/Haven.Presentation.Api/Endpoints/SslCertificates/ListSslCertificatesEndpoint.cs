using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.SslCertificates.Queries.GetSslCertificates;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.SslCertificates;

public sealed class ListSslCertificatesEndpoint(IMediator mediator)
    : EndpointWithoutRequest<ApiResponse<List<SslCertificateDto>>>
{
    public override void Configure()
    {
        Get("/ssl-certificates");

        Options(x => x.WithTags("SslCertificates"));
        Summary(s =>
        {
            s.Summary = "List SSL certificates";
            s.Description = "Lists every certificate in the SSL certificate library, along with how many domains it's currently attached to.";
            s[200] = "OK";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await mediator.Send(new GetSslCertificatesQuery(), ct);
        await this.SendResultAsync(result, ct);
    }
}