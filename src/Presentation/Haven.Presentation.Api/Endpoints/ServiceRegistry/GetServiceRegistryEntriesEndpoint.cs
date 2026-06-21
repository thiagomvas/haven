using FastEndpoints;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.ServiceRegistry.Queries.GetServiceRegistryEntries;
using Haven.Presentation.Api.Extensions;
using Mediator;

namespace Haven.Presentation.Api.Endpoints.ServiceRegistry;

public sealed class GetServiceRegistryEntriesEndpoint(IMediator mediator)
    : Endpoint<GetServiceRegistryEntriesQuery, PagedResult<ServiceRegistryEntryDto>>
{
    public override void Configure()
    {
        Get("/service-registry");
        AllowAnonymous();
        Options(x => x.WithTags("ServiceRegistry"));
        Summary(s =>
        {
            s.Summary = "List service registry entries";
            s.Description = "Returns a paginated list of service registry entries.";
            s[200] = "OK";
            s[204] = "No content";
        });
    }

    public override async Task HandleAsync(GetServiceRegistryEntriesQuery req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
