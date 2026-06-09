using FastEndpoints;

using Haven.Application.Features.System.Queries.GetBuildInfo;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.System;

public sealed class GetBuildInfoEndpoint(IMediator mediator) : EndpointWithoutRequest<BuildInfoDto>
{
    public override void Configure()
    {
        Get("/system/build-info");
        AllowAnonymous();
        Options(x => x.WithTags("System"));
        Summary(s =>
        {
            s.Summary = "Get build info";
            s.Description = "Returns information about the current Haven build, runtime, and database.";
            s[200] = "OK";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await mediator.Send(new GetBuildInfoQuery(), ct);
        await this.SendResultAsync(result, ct);
    }
}