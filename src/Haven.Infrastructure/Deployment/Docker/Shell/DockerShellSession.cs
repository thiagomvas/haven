using System.Runtime.InteropServices;

using Docker.DotNet;

using Haven.Application.Common.Interfaces.Shell;
using Haven.Domain.Enums;

namespace Haven.Infrastructure.Deployment.Docker.Shell;

/// <inheritdoc cref="IShellSession" />
internal sealed class DockerShellSession : IShellSession
{
    private readonly MultiplexedStream _stream;

    public DockerShellSession(Guid serviceId, ShellType shellType, MultiplexedStream stream)
    {
        ServiceId = serviceId;
        ShellType = shellType;
        SessionId = Guid.NewGuid();
        _stream = stream;
    }

    public Guid SessionId { get; }
    public Guid ServiceId { get; }
    public ShellType ShellType { get; }

    public Task WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        // Docker.DotNet's MultiplexedStream only exposes an array-segment write overload.
        if (!MemoryMarshal.TryGetArray(data, out var segment))
            segment = new ArraySegment<byte>(data.ToArray());

        return _stream.WriteAsync(segment.Array!, segment.Offset, segment.Count, cancellationToken);
    }

    public async Task<ShellReadResult> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        if (!MemoryMarshal.TryGetArray((ReadOnlyMemory<byte>)buffer, out var segment))
            throw new ArgumentException("Buffer must be backed by an array.", nameof(buffer));

        var result = await _stream.ReadOutputAsync(segment.Array!, segment.Offset, segment.Count, cancellationToken);
        return new ShellReadResult(result.Count, result.EOF);
    }

    public ValueTask DisposeAsync()
    {
        _stream.Dispose();
        return ValueTask.CompletedTask;
    }
}
