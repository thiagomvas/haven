using FastEndpoints;

using Haven.Application.Common.Contracts;
using Haven.Application.Features.Jobs.Queries.GetJobInfos;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Jobs;

public class GetJobInfosEndpoint(IMediator mediator) : EndpointWithoutRequest<IEnumerable<JobInfo>>
{
    public override void Configure()
    {
        Get("/jobs");
        Options(x => x.WithTags("Jobs"));
        Summary(s =>
        {
            s.Summary = "Get job infos";
            s.Description = "Returns all registered job infos with their names and i18n keys.";
            s[200] = "OK";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var query = new GetJobInfosQuery();
        var result = await mediator.Send(query, ct);
        await this.SendResultAsync(result, ct);
    }
}