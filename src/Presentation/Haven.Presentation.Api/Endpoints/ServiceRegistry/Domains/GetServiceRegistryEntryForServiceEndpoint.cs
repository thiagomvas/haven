using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.ServiceRegistry.Queries.GetServiceRegistryEntries;
using Haven.Application.Features.ServiceRegistry.Queries.GetServiceRegistryEntryForService;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.ServiceRegistry.Domains;

public sealed class GetServiceRegistryEntryForServiceEndpoint(IMediator mediator)
    : Endpoint<GetServiceRegistryEntryForServiceQuery, ApiResponse<ServiceRegistryEntryDto?>>
{
    public override void Configure()
    {
        Get("/service-registry/services/{serviceId}");

        Options(x => x.WithTags("ServiceRegistry"));
        Summary(s =>
        {
            s.Summary = "Get a service's registry entry";
            s.Description = "Returns the service registry entry (container name, IP, ports, and domains) for a single service, or null if the service has never been registered.";
            s[200] = "OK";
        });
    }

    public override async Task HandleAsync(GetServiceRegistryEntryForServiceQuery req, CancellationToken ct)
    {
        req.ServiceId = Route<Guid>("serviceId");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
