using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Common.Messaging;
using Haven.Domain.Enums;

namespace Haven.Application.Features.Dashboard.Queries.GetDashboardAttention;

public sealed class GetDashboardAttentionHandler(IProjectRepository repository, IServiceStateReader stateReader)
    : IQueryHandler<GetDashboardAttentionQuery, List<AttentionServiceDto>>
{
    private const int MaxLimit = 50;

    public async ValueTask<Result<List<AttentionServiceDto>>> Handle(GetDashboardAttentionQuery query, CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(query.Limit, 1, MaxLimit);
        var items = new List<AttentionServiceDto>();

        // Materialize first: the streaming reader would otherwise stay open while the state reader queries on the same connection.
        var projects = await repository.GetAsync(cancellationToken).ToListAsync(cancellationToken);

        foreach (var project in projects)
        {
            var services = project.Environments.SelectMany(e => e.Services).ToList();
            if (services.Count == 0)
                continue;

            var state = await stateReader.LoadAsync(services.Select(s => s.Id), cancellationToken);

            foreach (var environment in project.Environments)
            {
                foreach (var service in environment.Services)
                {
                    var reason = ResolveReason(state, service.Id);
                    if (reason is null)
                        continue;

                    items.Add(new AttentionServiceDto
                    {
                        ServiceId = service.Id,
                        ServiceName = service.Name,
                        ProjectId = project.Id,
                        ProjectName = project.Name,
                        EnvironmentId = environment.Id,
                        EnvironmentName = environment.Name,
                        Reason = reason.Value,
                        Since = reason switch
                        {
                            AttentionReason.Crashed => state.GetEntry(service.Id)?.StateChangedAt,
                            AttentionReason.DeploymentFailed => service.LastDeployedAt,
                            _ => null,
                        },
                    });
                }
            }
        }

        // Hard failures first, then the most recent problem first, then a stable name order.
        var ordered = items
            .OrderBy(i => i.Reason == AttentionReason.Unhealthy ? 1 : 0)
            .ThenByDescending(i => i.Since ?? DateTime.MinValue)
            .ThenBy(i => i.ProjectName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(i => i.ServiceName, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .ToList();

        return Result<List<AttentionServiceDto>>.Success(ordered);
    }

    /// <summary>Mirrors the overview's definition of "affected" so both widgets agree.</summary>
    private static AttentionReason? ResolveReason(ServiceStateSnapshot state, Guid serviceId)
    {
        var status = state.GetDisplayStatus(serviceId);
        if (status == ServiceDisplayStatus.Crashed)
            return AttentionReason.Crashed;
        if (status == ServiceDisplayStatus.Degraded)
            return AttentionReason.Unhealthy;
        if (state.HasFailedDeployment(serviceId))
            return AttentionReason.DeploymentFailed;
        return null;
    }
}
