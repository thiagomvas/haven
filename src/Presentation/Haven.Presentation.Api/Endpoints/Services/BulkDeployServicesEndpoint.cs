using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Services.Commands.BulkDeployServices;
using Haven.Application.Features.Services.Shared;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Services;

public sealed class BulkDeployServicesEndpoint(IMediator mediator)
    : Endpoint<BulkDeployServicesCommand, ApiResponse<BulkServiceActionResponse>>
{
    public override void Configure()
    {
        Post("/projects/{projectId}/environments/{environmentId}/services/bulk-deploy");
        Options(x => x.WithTags("Services"));
        Summary(s =>
        {
            s.Summary = "Deploy multiple services";
            s.Description = "Deploys each of the given services in its environment, returning a per-service result.";
            s[200] = "Success";
            s[400] = "Validation error";
            s[404] = "Project or environment not found";
        });
    }

    public override async Task HandleAsync(BulkDeployServicesCommand req, CancellationToken ct)
    {
        req.ProjectId = Route<Guid>("projectId");
        req.EnvironmentId = Route<Guid>("environmentId");
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
