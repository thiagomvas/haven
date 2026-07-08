using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Mappers;
using Haven.Domain.Entities;

namespace Haven.Application.Features.Services.Queries.ListVolumesForService;

public sealed class ListVolumesForServiceHandler(IServiceRepository serviceRepository)
    : IQueryHandler<ListVolumesForServiceQuery, IReadOnlyList<ServiceVolumeDto>>
{
    public async ValueTask<Result<IReadOnlyList<ServiceVolumeDto>>> Handle(ListVolumesForServiceQuery query, CancellationToken cancellationToken)
    {
        var service = await serviceRepository.GetByIdAsync(query.ServiceId, cancellationToken);
        if (service is null)
            return Error.NotFoundFor(nameof(Service), query.ServiceId);

        return Result<IReadOnlyList<ServiceVolumeDto>>.Success(service.Volumes.ToDtos());
    }
}