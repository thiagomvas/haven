using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Services.Commands.ApplyManifestForService;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Services;

public sealed class ApplyManifestForServiceEndpoint(IMediator mediator)
    : Endpoint<ApplyManifestForServiceCommand, ApiResponse>
{
    public override void Configure()
    {
        Put("/projects/{projectId:guid}/environments/{environmentId:guid}/services/{serviceId:guid}/manifest");
        Options(x => x.WithTags("Services"));
        Summary(s =>
        {
            s.Summary = "Apply a manifest to a service";
            s.Description = "Parses the provided YAML manifest and updates the service to match it. The manifest file on disk is also updated via domain events.";
            s[200] = "Applied";
            s[400] = "Validation error";
            s[404] = "Service, environment, or project not found";
            s[409] = "Service name conflict";
        });
    }

    public override async Task HandleAsync(ApplyManifestForServiceCommand req, CancellationToken ct)
    {
        req.ProjectId = Route<Guid>("projectId");
        req.EnvironmentId = Route<Guid>("environmentId");
        req.ServiceId = Route<Guid>("serviceId");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}