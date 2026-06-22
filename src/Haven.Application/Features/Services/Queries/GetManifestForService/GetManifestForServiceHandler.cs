using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.Entities;

namespace Haven.Application.Features.Services.Queries.GetManifestForService;

public class GetManifestForServiceHandler(IServiceRepository repository, IManifestSerializer<Service> serializer) : IQueryHandler<GetManifestForServiceQuery, string>
{
    public async ValueTask<Result<string>> Handle(GetManifestForServiceQuery query, CancellationToken cancellationToken)
    {
        var service = await repository.GetByIdAsync(query.ServiceId, cancellationToken);
        if (service is null)
        {
            return Error.NotFoundFor(nameof(Service), query.ServiceId);
        }

        return await serializer.ReadManifestAsync(service, cancellationToken);
    }
}