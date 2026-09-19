using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Mappers;
using Haven.Domain.Entities;

namespace Haven.Application.Features.HealthChecks.Queries.GetHealthCheckResultsQuery;

public sealed class GetHealthCheckResultsHandler(IHealthCheckRepository healthCheckRepository)
    : IQueryHandler<GetHealthCheckResultsQuery, IReadOnlyList<HealthCheckResultDto>>
{
    public async ValueTask<Result<IReadOnlyList<HealthCheckResultDto>>> Handle(GetHealthCheckResultsQuery query, CancellationToken cancellationToken)
    {
        var healthCheck = await healthCheckRepository.GetByIdAsync(query.HealthCheckId, cancellationToken);
        if (healthCheck is null)
            return Error.NotFoundFor(nameof(HealthCheck), query.HealthCheckId);

        var results = await healthCheckRepository.GetResultsAsync(query.HealthCheckId, query.Limit, cancellationToken);
        return Result<IReadOnlyList<HealthCheckResultDto>>.Success(results.ToDtos());
    }
}
