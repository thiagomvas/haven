using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Mappers;

namespace Haven.Application.Features.HealthChecks.Queries.GetServiceHealthChecksQuery;

public class GetServiceHealthChecksHandler(IHealthCheckRepository healthCheckRepository)
    : IQueryHandler<GetServiceHealthChecksQuery, IReadOnlyList<HealthCheckDto>>
{
    public async ValueTask<Result<IReadOnlyList<HealthCheckDto>>> Handle(GetServiceHealthChecksQuery query, CancellationToken cancellationToken)
    {
        var healthChecks = await healthCheckRepository.GetForServiceListAsync(query.ServiceId, cancellationToken);
        return Result<IReadOnlyList<HealthCheckDto>>.Success(healthChecks.ToDtos());
    }
}