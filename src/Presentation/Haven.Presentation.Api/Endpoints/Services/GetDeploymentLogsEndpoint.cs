using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Deployments.Queries.GetDeploymentLogs;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Services;

public sealed class GetDeploymentLogsEndpoint(IMediator mediator)
    : Endpoint<GetDeploymentLogsQuery, ApiResponse<string[]>>
{
    public override void Configure()
    {
        Get("/deployments/{deploymentId}/logs");
        AllowAnonymous();
        Options(x => x.WithTags("Services"));
        Summary(s =>
        {
            s.Summary = "Get deployment logs";
            s.Description = "Returns log lines for a deployment from its log file.";
            s[200] = "OK";
            s[404] = "Deployment not found";
        });
    }

    public override async Task HandleAsync(GetDeploymentLogsQuery req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
