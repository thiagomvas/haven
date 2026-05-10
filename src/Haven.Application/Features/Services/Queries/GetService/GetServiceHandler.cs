using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Mappers;
using Haven.Domain.Aggregates;

namespace Haven.Application.Features.Services.Queries.GetService;

public sealed class GetServiceHandler(
    IProjectRepository projectRepository,
    IEnvironmentRepository environmentRepository,
    IServiceRepository serviceRepository)
    : IQueryHandler<GetServiceQuery, ServiceDto>
{
    public async ValueTask<Result<ServiceDto>> Handle(GetServiceQuery query, CancellationToken cancellationToken)
    {
        var projectExists = await projectRepository.GetByIdAsync(query.ProjectId, cancellationToken) is not null;
        if (!projectExists)
            return Error.NotFoundFor(nameof(Project), query.ProjectId);

        var environment = await environmentRepository.GetByIdAsync(query.EnvironmentId, cancellationToken);
        if (environment is null || environment.ProjectId != query.ProjectId)
            return Error.NotFoundFor("Environment", query.EnvironmentId);

        var service = await serviceRepository.GetByIdAsync(query.ServiceId, cancellationToken);
        if (service is null || service.EnvironmentId != query.EnvironmentId)
            return Error.NotFoundFor("Service", query.ServiceId);

        var dto = service.ToDto();

        return Result<ServiceDto>.Success(dto);
    }
}
