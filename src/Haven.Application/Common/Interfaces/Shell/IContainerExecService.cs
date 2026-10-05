namespace Haven.Application.Common.Interfaces.Shell;

/// <summary>
/// Runs a one-off, non-interactive command (<c>docker exec</c> without a TTY) inside the container
/// backing a <see cref="Haven.Domain.Aggregates.Service"/> and waits for it to finish.
/// </summary>
public interface IContainerExecService
{
    /// <summary>
    /// Executes <paramref name="command"/> (argv, no shell interpretation) inside the running container for
    /// <paramref name="serviceId"/>. Fails with <see cref="Error.Docker"/>.ContainerNotFound when no container
    /// exists, or <see cref="Error.Docker"/>.OperationFailed when it isn't running. A non-zero exit code is
    /// still a successful result; callers decide how to interpret it.
    /// </summary>
    Task<Result<ContainerExecResult>> ExecAsync(Guid serviceId, IReadOnlyList<string> command, string? workingDir,
        string? user, TimeSpan timeout, CancellationToken cancellationToken);
}

public sealed record ContainerExecResult(long ExitCode, string StdOut, string StdErr);