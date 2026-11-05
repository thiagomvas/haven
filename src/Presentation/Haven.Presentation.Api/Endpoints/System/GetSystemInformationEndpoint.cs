using FastEndpoints;

using Haven.Application.Common.Contracts;
using Haven.Application.Common.Responses;
using Haven.Application.Features.System.Queries.GetSystemInformation;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.System;

public class GetSystemInformationEndpoint(IMediator mediator) : EndpointWithoutRequest<ApiResponse<SystemInformation>>
{
    public override void Configure()
    {
        Get("/system/info");

        Options(x => x.WithTags("System"));
        Summary(s =>
        {
            s.Summary = "Get system information";
            s.Description = "Returns host details: operating system and version, uptime, CPU cores, RAM and storage (MB) and IP address.";
            s[200] = "System information";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await mediator.Send(new GetSystemInformationQuery(), ct);
        await this.SendResultAsync(result, ct);
    }
}