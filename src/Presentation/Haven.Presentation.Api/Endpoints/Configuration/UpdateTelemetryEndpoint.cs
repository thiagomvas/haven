using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Configuration;
using Haven.Application.Features.Configuration.Commands.UpdateTelemetry;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Configuration;

public sealed class UpdateTelemetryEndpoint(IMediator mediator)
    : Endpoint<UpdateTelemetryCommand, ApiResponse<TelemetryOptions>>
{
    public override void Configure()
    {
        Put("/configuration/telemetry");
        Options(x => x.WithTags("Configuration"));
        Summary(s =>
        {
            s.Summary = "Update telemetry configuration";
            s.Description = "Persists OpenTelemetry export configuration to the database.";
            s[200] = "Updated";
            s[400] = "Validation error";
            s[403] = "Forbidden";
        });
    }

    public override async Task HandleAsync(UpdateTelemetryCommand req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}