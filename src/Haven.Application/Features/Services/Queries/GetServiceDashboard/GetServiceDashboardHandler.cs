using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.Projects.Queries.GetProjectsDashboard;
using Haven.Application.Mappers;
using Haven.Domain.Aggregates;

namespace Haven.Application.Features.Services.Queries.GetServiceDashboard;

public sealed class GetServiceDashboardHandler(
    IServiceRepository serviceRepository,
    IEnvironmentVariableService environmentVariableService,
    IFeatureFlagRepository featureFlagRepository)
    : IQueryHandler<GetServiceDashboardQuery, ServiceDashboardDto>
{
    public async ValueTask<Result<ServiceDashboardDto>> Handle(GetServiceDashboardQuery query, CancellationToken cancellationToken)
    {
        var service = await serviceRepository.GetByIdAsync(query.ServiceId, cancellationToken);
        if (service is null || service.EnvironmentId != query.EnvironmentId)
            return Error.NotFoundFor("Service", query.ServiceId);

        var dto = service.ToDashboardDto();
        var envVars = await environmentVariableService.BuildVariablesForServiceAsync(query.ServiceId, cancellationToken);
        dto.EnvironmentVariables = envVars.Select(ev => ev.ToDto()).ToList();

        var flags = await featureFlagRepository.GetForServiceListAsync(query.ServiceId, cancellationToken);
        dto.FeatureFlags = flags.Select(ff => ff.ToDto()).ToList();
        
        return Result<ServiceDashboardDto>.Success(dto);
    }
}
