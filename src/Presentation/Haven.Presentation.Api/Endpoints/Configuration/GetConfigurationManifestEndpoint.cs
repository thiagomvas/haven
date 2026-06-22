using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Configuration.Queries.GetConfigurationManifest;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Configuration;

public sealed class GetConfigurationManifestEndpoint(IMediator mediator)
    : EndpointWithoutRequest<ApiResponse<string>>
{
    public override void Configure()
    {
        Get("/configuration/manifest");
        Options(x => x.WithTags("Configuration"));
        Summary(s =>
        {
            s.Summary = "Get Haven configuration manifest";
            s.Description = "Returns the raw YAML content of the haven.yml configuration file.";
            s[200] = "Manifest retrieved";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await mediator.Send(new GetConfigurationManifestQuery(), ct);
        await this.SendResultAsync(result, ct);
    }
}
