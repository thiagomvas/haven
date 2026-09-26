using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.ServiceTemplates.Commands.CreateServiceFromTemplate;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.ServiceTemplates;

public sealed class CreateServiceFromTemplateEndpoint(IMediator mediator)
    : Endpoint<CreateServiceFromTemplateCommand, ApiResponse<Guid>>
{
    public override void Configure()
    {
        Post("/projects/{projectId}/environments/{environmentId}/services/from-template/{templateId}");
        Options(x => x.WithTags("Services"));
        Summary(s =>
        {
            s.Summary = "Create a service from a template";
            s.Description = "Instantiates a new service in an environment from a built-in service template (e.g. Postgres, Redis).";
            s[201] = "Created";
            s[422] = "Validation error";
            s[404] = "Environment or template not found";
            s[409] = "Service name or alias already in use";
        });
    }

    public override async Task HandleAsync(CreateServiceFromTemplateCommand req, CancellationToken ct)
    {
        req.ProjectId = Route<Guid>("projectId");
        req.EnvironmentId = Route<Guid>("environmentId");
        req.TemplateId = Route<string>("templateId")!;
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
