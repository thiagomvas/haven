using Haven.Application.Common.Responses;
using Haven.Application.Features.Configuration.Dtos;
using Haven.Application.Features.Configuration.Queries.GetConfiguration;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Configuration;

using FastEndpoints;

public sealed class GetConfigurationEndpoint(IMediator mediator)
    : Endpoint<EmptyRequest, ApiResponse<HavenConfigurationDto>>
{
    public override void Configure()
    {
        Get("/configuration");
        
        Options(x => x.WithTags("Configuration"));
        Summary(s =>
        {
            s.Summary = "Get Haven configuration";
            s[200] = "Configuration retrieved";
        });
    }

    public override async Task HandleAsync(EmptyRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new GetConfigurationQuery(), ct);
        await this.SendResultAsync(result, ct);
    }
}