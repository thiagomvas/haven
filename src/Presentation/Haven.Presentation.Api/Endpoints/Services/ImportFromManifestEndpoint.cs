using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Services.Commands.ImportFromManifest;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Services;

public class ImportFromManifestEndpoint(IMediator mediator) : Endpoint<ImportFromManifestRequest, ApiResponse<Guid>>
{
    public override void Configure()
    {
        Post("services/import-from-manifest");

        Options(x => x.WithTags("Services"));
        Summary(s =>
        {
            s.Summary = "Import a service from a manifest";
            s.Description = "Creates a service in the given environment from a raw YAML service manifest and returns the new service id.";
            s[200] = "Service created; returns its id";
            s[400] = "Invalid manifest or validation error";
            s[404] = "Environment not found";
        });
    }

    public override async Task HandleAsync(ImportFromManifestRequest req, CancellationToken ct)
    {
        var command = new ImportFromManifestCommand
        {
            EnvironmentId = req.EnvironmentId,
            RawManifest = req.RawManifest
        };

        var result = await mediator.Send(command, ct);
        await this.SendResultAsync(result, ct);
    }
}

public class ImportFromManifestRequest
{
    public Guid EnvironmentId { get; set; }
    public string? RawManifest { get; set; }
}