using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Projects.Commands.ImportProject;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Projects;

public sealed class ImportProjectEndpoint(IMediator mediator) : Endpoint<ImportProjectCommand, ApiResponse<Guid>>
{
    public override void Configure()
    {
        Post("/projects/import");
        Options(x => x.WithTags("Projects"));
        Summary(s =>
        {
            s.Summary = "Import a project from a manifest";
            s.Description = "Creates a new project from a project manifest, optionally including its environments and their services. New ids are always generated.";
            s[201] = "Created";
            s[422] = "Validation error or invalid manifest";
            s[409] = "A project with that name already exists";
        });
    }

    public override async Task HandleAsync(ImportProjectCommand req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
