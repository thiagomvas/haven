using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.System.Queries.GetAllPermissions;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.System;

public sealed class GetAllPermissionsEndpoint(IMediator mediator) : EndpointWithoutRequest<ApiResponse<string[]>>
{
    public override void Configure()
    {
        Get("/system/permissions");
        Options(x => x.WithTags("System"));
        Summary(s =>
        {
            s.Summary = "Get all permissions";
            s.Description = "Returns the list of all available permissions in the system.";
            s[200] = "Permissions returned";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await mediator.Send(new GetAllPermissionsQuery(), ct);
        await this.SendResultAsync(result, ct);
    }
}