using FastEndpoints;

using Haven.Application.Features.System.Queries.GetLatestVersion;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.System;

public sealed class GetLatestVersionEndpoint(IMediator mediator) : EndpointWithoutRequest<LatestVersionDto>
{
    public override void Configure()
    {
        Get("/system/latest-version");

        Options(x => x.WithTags("System"));
        Summary(s =>
        {
            s.Summary = "Get latest Haven version";
            s.Description = "Returns the latest known Haven release and whether an update is available.";
            s[200] = "OK";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await mediator.Send(new GetLatestVersionQuery(), ct);
        await this.SendResultAsync(result, ct);
    }
}