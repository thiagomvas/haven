using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Services.Queries;
using Haven.Application.Features.Services.Queries.GetServiceComputedOutputValue;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Services;

public sealed class GetServiceComputedOutputValueEndpoint(IMediator mediator)
    : Endpoint<GetServiceComputedOutputValueQuery, ApiResponse<ComputedOutputValueDto>>
{
    public override void Configure()
    {
        Get("/projects/{projectId}/environments/{environmentId}/services/{serviceId}/outputs/{key}/value");

        Options(x => x.WithTags("Services"));
        Summary(s =>
        {
            s.Summary = "Reveal a computed output's value";
            s.Description = "Returns the resolved plaintext value of a computed output (e.g. a connection string). " +
                             "Requires the 'manage secrets' permission, since the value may embed a decrypted secret.";
            s[200] = "OK";
            s[403] = "Caller lacks the 'manage secrets' permission";
            s[404] = "Project, environment, service, or output not found";
        });
    }

    public override async Task HandleAsync(GetServiceComputedOutputValueQuery req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
