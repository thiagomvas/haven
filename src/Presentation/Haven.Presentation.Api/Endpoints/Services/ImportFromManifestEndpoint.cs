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