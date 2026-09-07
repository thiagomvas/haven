using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Jobs.Commands.TriggerJob;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Jobs;

public class TriggerJobEndpoint(IMediator mediator) : Endpoint<TriggerJobRequest, ApiResponse>
{
    public override void Configure()
    {
        Post("/jobs/trigger");
        Options(x => x.WithTags("Jobs"));
        Summary(s =>
        {
            s.Summary = "Trigger a job";
            s.Description = "Triggers a job by its key.";
            s[200] = "OK";
        });
    }

    public override async Task HandleAsync(TriggerJobRequest req, CancellationToken ct)
    {
        var command = new TriggerJobCommand(req.JobKey);
        var result = await mediator.Send(command, ct);
        await this.SendResultAsync(result, ct);
    }
}

public class TriggerJobRequest
{
    public string JobKey { get; set; } = string.Empty;
}