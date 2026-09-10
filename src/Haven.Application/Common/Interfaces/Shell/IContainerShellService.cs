using Haven.Domain.Enums;

namespace Haven.Application.Common.Interfaces.Shell;

/// <summary>
/// Creates interactive shell sessions (<c>docker exec</c> with a TTY attached) inside the
/// container backing a <see cref="Haven.Domain.Aggregates.Service"/>. This is the sole
/// responsibility of this service; session lifetime, I/O piping and transport (e.g. SignalR)
/// are handled by callers.
/// </summary>
public interface IContainerShellService
{
    /// <summary>
    /// Locates the running container for <paramref name="serviceId"/> and starts an interactive
    /// <paramref name="shellType"/> exec inside it. Fails with <see cref="Error.Docker"/>.ContainerNotFound
    /// when no container exists for the service, or with <see cref="Error.Docker"/>.OperationFailed
    /// when the container exists but isn't running.
    /// </summary>
    Task<Result<IShellSession>> CreateSessionAsync(Guid serviceId, ShellType shellType, CancellationToken cancellationToken);
}
