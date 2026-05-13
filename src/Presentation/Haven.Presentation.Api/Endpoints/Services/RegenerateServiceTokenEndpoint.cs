using FastEndpoints;
using Haven.Application.Common.Responses;
using Haven.Application.Features.Services.Commands.RegenerateServiceToken;
using Haven.Presentation.Api.Extensions;
using Mediator;

namespace Haven.Presentation.Api.Endpoints.Services;

public sealed class RegenerateServiceTokenEndpoint(IMediator mediator)
    : Endpoint<RegenerateServiceTokenCommand, ApiResponse<string>>
{
    public override void Configure()
    {
        Post("/projects/{projectId}/environments/{environmentId}/services/{serviceId}/tokens/regenerate");
        AllowAnonymous();
        Options(x => x.WithTags("Services"));
        Summary(s =>
        {
            s.Summary = "Regenerate service token";
            s.Description = "Regenerates the deployment token for a service and returns the new token.";
            s[200] = "Token regenerated successfully";
            s[400] = "Validation error";
            s[404] = "Project, environment, or service not found";
        });
    }

    public override async Task HandleAsync(RegenerateServiceTokenCommand req, CancellationToken ct)
    {
        req.ProjectId = Route<Guid>("projectId");
        req.EnvironmentId = Route<Guid>("environmentId");
        req.ServiceId = Route<Guid>("serviceId");
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
