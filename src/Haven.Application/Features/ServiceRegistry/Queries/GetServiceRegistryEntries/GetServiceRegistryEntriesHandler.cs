using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Mappers;

namespace Haven.Application.Features.ServiceRegistry.Queries.GetServiceRegistryEntries;

public sealed class GetServiceRegistryEntriesHandler(IServiceRegistryEntryRepository repository)
    : IPagedQueryHandler<GetServiceRegistryEntriesQuery, ServiceRegistryEntryDto>
{
    public async ValueTask<PagedResult<ServiceRegistryEntryDto>> Handle(GetServiceRegistryEntriesQuery query, CancellationToken cancellationToken)
    {
        var paged = await repository.GetPagedAsync(query.PageNumber, query.PageSize, query.Search, cancellationToken);
        return paged.Project(e => e.ToRegistryDto());
    }
}