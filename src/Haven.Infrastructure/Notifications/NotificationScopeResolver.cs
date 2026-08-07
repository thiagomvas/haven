using Haven.Application.Common.Interfaces;
using Haven.Domain;
using Haven.Domain.Enums;
using Haven.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Haven.Infrastructure.Notifications;

public class NotificationScopeResolver(HavenDbContext context) : INotificationScopeResolver
{
    public async Task<IReadOnlyList<(NotificationScope Scope, Guid ScopeId)>> ResolveChainAsync(
        NotificationScope primaryScope, Guid primaryScopeId, CancellationToken cancellationToken = default)
    {
        return primaryScope switch
        {
            NotificationScope.Service => await ResolveServiceChainAsync(primaryScopeId, cancellationToken),
            NotificationScope.Environment => await ResolveEnvironmentChainAsync(primaryScopeId, cancellationToken),
            NotificationScope.Project => [(NotificationScope.Project, primaryScopeId)],
            _ => [],
        };
    }

    private async Task<IReadOnlyList<(NotificationScope, Guid)>> ResolveServiceChainAsync(Guid serviceId, CancellationToken ct)
    {
        var ids = await context.Services
            .Where(s => s.Id == serviceId)
            .Select(s => new { s.EnvironmentId })
            .FirstOrDefaultAsync(ct);

        if (ids is null)
            return [(NotificationScope.Service, serviceId)];

        var projectId = await context.Environments
            .Where(e => e.Id == ids.EnvironmentId)
            .Select(e => e.ProjectId)
            .FirstOrDefaultAsync(ct);

        return
        [
            (NotificationScope.Service, serviceId),
            (NotificationScope.Environment, ids.EnvironmentId),
            (NotificationScope.Project, projectId),
        ];
    }

    private async Task<IReadOnlyList<(NotificationScope, Guid)>> ResolveEnvironmentChainAsync(Guid environmentId, CancellationToken ct)
    {
        var projectId = await context.Environments
            .Where(e => e.Id == environmentId)
            .Select(e => e.ProjectId)
            .FirstOrDefaultAsync(ct);

        return
        [
            (NotificationScope.Environment, environmentId),
            (NotificationScope.Project, projectId),
        ];
    }
}