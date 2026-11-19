using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Services.Commands.ImportFromRepository;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Services;

public class ImportFromRepositoryEndpoint(IMediator mediator) : Endpoint<ImportFromRepositoryCommand, ApiResponse<Guid>>
{
    public override void Configure()
    {
        Post("services/import-from-repository");

        Options(x => x.WithTags("Services"));
        Summary(s =>
        {
            s.Summary = "Import a service from a Git repository";
            s.Description = "Fetches a service manifest from FilePath on the given Branch of a Git repository (optionally using a stored Git credential) and creates the service in the environment. Returns the new service id.";
            s[200] = "Service created; returns its id";
            s[400] = "Invalid manifest or validation error";
            s[404] = "Environment or Git credential not found";
        });
    }

    public override async Task HandleAsync(ImportFromRepositoryCommand req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}