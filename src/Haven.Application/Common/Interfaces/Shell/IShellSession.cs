using Haven.Domain.Enums;

namespace Haven.Application.Common.Interfaces.Shell;

/// <summary>
/// A single interactive shell exec attached to a running container. Bidirectional: input written
/// via <see cref="WriteAsync"/> goes to the shell's stdin, output is drained via
/// <see cref="ReadAsync"/>. Callers (e.g. a SignalR hub) own the read/write loop and are
/// responsible for disposing the session once the client disconnects or the shell exits.
/// </summary>
public interface IShellSession : IAsyncDisposable
{
    Guid SessionId { get; }
    Guid ServiceId { get; }
    ShellType ShellType { get; }

    /// <summary>Writes raw bytes to the shell's stdin (e.g. keystrokes from the client).</summary>
    Task WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken);

    /// <summary>
    /// Reads the next chunk of combined stdout/stderr output into <paramref name="buffer"/>.
    /// Returns 0 bytes with <see cref="ShellReadResult.Eof"/> set once the shell process exits.
    /// </summary>
    Task<ShellReadResult> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken);
}

public readonly record struct ShellReadResult(int Count, bool Eof);
