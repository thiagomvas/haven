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
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await mediator.Send(new GetSystemInformationQuery(), ct);
        await this.SendResultAsync(result, ct);
    }
}