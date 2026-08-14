using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.ServiceRegistry.Queries.GetServiceRegistryEntries;
using Haven.Application.Mappers;

namespace Haven.Application.Features.ServiceRegistry.Queries.GetServiceRegistryEntryForService;

public sealed class GetServiceRegistryEntryForServiceHandler(IServiceRegistryEntryRepository serviceRegistryEntryRepository)
    : IQueryHandler<GetServiceRegistryEntryForServiceQuery, ServiceRegistryEntryDto?>
{
    public async ValueTask<Result<ServiceRegistryEntryDto?>> Handle(GetServiceRegistryEntryForServiceQuery query, CancellationToken cancellationToken)
    {
        var entry = await serviceRegistryEntryRepository.GetForServiceAsync(query.ServiceId, cancellationToken);
        return Result<ServiceRegistryEntryDto?>.Success(entry?.ToRegistryDto());
    }
}