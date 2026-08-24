using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.ServiceRegistry.Queries.GetServiceRegistryEntries;
using Haven.Application.Mappers;

namespace Haven.Application.Features.Sidecars.Queries.GetSidecarDomains;

public sealed class GetSidecarDomainsHandler(IServiceRegistryEntryRepository serviceRegistryEntryRepository)
    : IQueryHandler<GetSidecarDomainsQuery, List<ServiceRegistryDomainDto>>
{
    public async ValueTask<Result<List<ServiceRegistryDomainDto>>> Handle(GetSidecarDomainsQuery query, CancellationToken cancellationToken)
    {
        var entry = await serviceRegistryEntryRepository.GetForSidecarAsync(query.SidecarId, cancellationToken);
        return Result<List<ServiceRegistryDomainDto>>.Success(entry?.Domains.ToDtos() ?? []);
    }
}
