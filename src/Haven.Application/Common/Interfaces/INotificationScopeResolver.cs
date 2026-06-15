using Haven.Domain;

namespace Haven.Application.Common.Interfaces;

public interface INotificationScopeResolver
{
    Task<IReadOnlyList<(NotificationScope Scope, Guid ScopeId)>> ResolveChainAsync(
        NotificationScope primaryScope, Guid primaryScopeId, CancellationToken cancellationToken = default);
}
