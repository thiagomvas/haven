using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Deployments.Commands.CancelDeployment;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Services;

public sealed class CancelDeploymentEndpoint(IMediator mediator)
    : Endpoint<CancelDeploymentCommand, ApiResponse>
{
    public override void Configure()
    {
        Post("/deployments/{deploymentId}/cancel");
        Options(x => x.WithTags("Deployments"));
        Summary(s =>
        {
            s.Summary = "Cancel a deployment";
            s.Description = "Cancels an in-progress deployment.";
            s[200] = "Success";
            s[400] = "Deployment is not in progress";
            s[404] = "Deployment not found";
        });
    }

    public override async Task HandleAsync(CancelDeploymentCommand req, CancellationToken ct)
    {
        req.DeploymentId = Route<Guid>("deploymentId");
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
