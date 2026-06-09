using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.System.Commands.RestartHaven;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.System;

public sealed class RestartEndpoint(IMediator mediator) : EndpointWithoutRequest<ApiResponse>
{
    public override void Configure()
    {
        Post("/system/restart");
        Options(x => x.WithTags("System"));
        Summary(s =>
        {
            s.Summary = "Restart Haven";
            s.Description = "Gracefully stops the application. Expects the process manager to restart it. Admin only.";
            s[200] = "Restart initiated";
            s[403] = "Forbidden. Admin only";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await mediator.Send(new RestartHavenCommand(), ct);
        await this.SendResultAsync(result, ct);
    }
}