using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Configuration.Commands.ApplyConfigurationManifest;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Configuration;

public sealed class ApplyConfigurationManifestEndpoint(IMediator mediator)
    : Endpoint<ApplyConfigurationManifestCommand, ApiResponse>
{
    public override void Configure()
    {
        Put("/configuration/manifest");
        Options(x => x.WithTags("Configuration"));
        Summary(s =>
        {
            s.Summary = "Apply Haven configuration manifest";
            s.Description = "Parses the provided YAML and updates haven.yml and the running configuration to match it.";
            s[200] = "Applied";
            s[400] = "Validation error";
        });
    }

    public override async Task HandleAsync(ApplyConfigurationManifestCommand req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
