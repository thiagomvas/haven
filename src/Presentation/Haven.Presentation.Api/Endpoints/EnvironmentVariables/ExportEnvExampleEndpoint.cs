using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.EnvironmentVariables.Queries.ExportEnvExample;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.EnvironmentVariables;

public class ExportEnvExampleEndpoint(IMediator mediator) : Endpoint<ExportEnvExampleQuery, ApiResponse<string>>
{
    public override void Configure()
    {
        Get("env/export-example");
        Options(x => x.WithTags("Environment Variables"));
        Summary(s =>
        {
            s.Summary = "Export environment variables example";
            s.Description = "Export environment variables example for a project, environment or service.";
            s.Responses[200] = "Environment variables example exported successfully";
            s.Responses[400] = "Invalid request";
            s.Responses[404] = "Parent not found";
        });
    }

    public override async Task HandleAsync(ExportEnvExampleQuery req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}