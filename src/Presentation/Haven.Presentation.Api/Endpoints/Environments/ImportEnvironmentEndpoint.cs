using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Environments.Commands.ImportEnvironment;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Environments;

public sealed class ImportEnvironmentEndpoint(IMediator mediator) : Endpoint<ImportEnvironmentCommand, ApiResponse<Guid>>
{
    public override void Configure()
    {
        Post("/projects/{projectId}/environments/import");
        Options(x => x.WithTags("Environments"));
        Summary(s =>
        {
            s.Summary = "Import an environment from a manifest";
            s.Description = "Creates a new environment in the project from an environment manifest, optionally including its services. New ids are always generated.";
            s[201] = "Created";
            s[422] = "Validation error or invalid manifest";
            s[404] = "Project not found";
            s[409] = "An environment with that name or alias already exists in the project";
        });
    }

    public override async Task HandleAsync(ImportEnvironmentCommand req, CancellationToken ct)
    {
        req.ProjectId = Route<Guid>("projectId");
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
