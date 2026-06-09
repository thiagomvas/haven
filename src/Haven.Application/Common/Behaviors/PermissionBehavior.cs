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
        var isAdminOnly = AdminOnlyCache.GetOrAdd(
            typeof(TMessage),
            static t => t.GetCustomAttribute<AdminOnlyAttribute>() is not null);

        if (isAdminOnly)
        {
            if (!currentUserService.IsAdmin)
                throw new ForbiddenException();
            return await next(message, ct);
        }

        var required = PermissionCache.GetOrAdd(
            typeof(TMessage),
            static t => t.GetCustomAttributes<RequirePermissionAttribute>()
                         .Select(a => a.Permission)
                         .ToArray());

        if (required.Length == 0)
            return await next(message, ct);

        var userId = currentUserService.UserId
            ?? throw new ForbiddenException();

        foreach (var permission in required)
        {
            if (!await permissionRepository.UserHasPermissionAsync(userId, permission, ct))
                throw new ForbiddenException();
        }

        return await next(message, ct);
    }
}