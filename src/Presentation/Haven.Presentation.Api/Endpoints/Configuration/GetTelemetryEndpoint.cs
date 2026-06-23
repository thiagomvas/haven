using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Configuration;
using Haven.Application.Features.Configuration.Queries.GetTelemetry;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Configuration;

public sealed class GetTelemetryEndpoint(IMediator mediator)
    : EndpointWithoutRequest<ApiResponse<TelemetryOptions>>
{
    public override void Configure()
    {
        Get("/configuration/telemetry");
        Options(x => x.WithTags("Configuration"));
        Summary(s =>
        {
            s.Summary = "Get telemetry configuration";
            s.Description = "Returns the current OpenTelemetry export configuration.";
            s[200] = "Success";
            s[403] = "Forbidden";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await mediator.Send(new GetTelemetryQuery(), ct);
        await this.SendResultAsync(result, ct);
    }
}
