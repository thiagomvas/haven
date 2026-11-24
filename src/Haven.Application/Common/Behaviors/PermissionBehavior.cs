using System.Collections.Concurrent;
using System.Reflection;

using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Exceptions;

using Mediator;

namespace Haven.Application.Common.Behaviors;

public sealed class PermissionBehavior<TMessage, TResponse>(
    ICurrentUserService currentUserService,
    IPermissionRepository permissionRepository)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    private static readonly ConcurrentDictionary<Type, string[]> PermissionCache = new();
    private static readonly ConcurrentDictionary<Type, bool> AdminOnlyCache = new();

    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken ct)
    {
        if (currentUserService.IsBackgroundContext)
            return await next(message, ct);

        var isAdminOnly = AdminOnlyCache.GetOrAdd(
            typeof(TMessage),
            static t => t.GetCustomAttribute<AdminOnlyAttribute>() is not null);

        if (isAdminOnly)
        {
            if (currentUserService.IsApiToken)
            {
                // A token only carries admin privilege when it was issued with the admin scope
                // and its owner is still an admin.
                var ownerId = currentUserService.UserId ?? throw new ForbiddenException();
                if (!currentUserService.Scopes.Contains(Permissions.AdminScope)
                    || !await permissionRepository.IsUserAdminAsync(ownerId, ct))
                    throw new ForbiddenException();
            }
            else if (!currentUserService.IsAdmin)
            {
                throw new ForbiddenException();
            }

            return await next(message, ct);
        }

        var required = PermissionCache.GetOrAdd(
            typeof(TMessage),
            static t => t.GetCustomAttributes<RequirePermissionAttribute>()
                .Select(a => a.Permission)
                .ToArray());

        if (required.Length > 0)
        {
            await PermissionGuard.EnsureAsync(currentUserService, permissionRepository, required, ct);
            return await next(message, ct);
        }

        if (typeof(TMessage).IsDefined(typeof(AnonymousAccessAttribute), inherit: false))
            return await next(message, ct);

        var authenticatedOnly = typeof(TMessage).GetCustomAttribute<AuthenticatedOnlyAttribute>(inherit: false);
        if (authenticatedOnly is not null)
        {
            if (currentUserService.UserId is null)
                throw new ForbiddenException();

            if (!authenticatedOnly.AllowApiTokens && currentUserService.IsApiToken)
                throw new ForbiddenException();

            return await next(message, ct);
        }

        // Fail closed: a message that declares no access rule must never run by accident.
        throw new InvalidOperationException(
            $"{typeof(TMessage).FullName} declares no access rule. Add [RequirePermission], [AdminOnly], " +
            "[AuthenticatedOnly] or [AnonymousAccess].");
    }
}