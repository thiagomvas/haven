using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Services.Queries.ResolveService;

public sealed class ResolveServiceHandler(IServiceRepository serviceRepository)
    : IQueryHandler<ResolveServiceQuery, ServiceLocationDto>
{
    public async ValueTask<Result<ServiceLocationDto>> Handle(ResolveServiceQuery query, CancellationToken cancellationToken)
    {
        var service = await serviceRepository.GetByIdAsync(query.ServiceId, cancellationToken);
        if (service is null)
            return Error.NotFoundFor("Service", query.ServiceId);

        return Result<ServiceLocationDto>.Success(new ServiceLocationDto
        {
            ServiceId = service.Id,
            EnvironmentId = service.EnvironmentId,
            ProjectId = service.Environment.ProjectId,
        });
    }
}