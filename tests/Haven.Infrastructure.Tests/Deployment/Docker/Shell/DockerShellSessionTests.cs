using Docker.DotNet;

using Haven.Domain.Enums;
using Haven.Infrastructure.Deployment.Docker.Shell;

using Shouldly;

namespace Haven.Infrastructure.Tests.Deployment.Docker.Shell;

[Category("Unit")]
public sealed class DockerShellSessionTests
{
    [Test]
    public void Constructor_ShouldAssignServiceIdAndShellType()
    {
        var serviceId = Guid.NewGuid();
        var stream = new MultiplexedStream(new MemoryStream(), multiplexed: false);

        var sut = new DockerShellSession(serviceId, ShellType.Sh, stream);

        sut.ServiceId.ShouldBe(serviceId);
        sut.ShellType.ShouldBe(ShellType.Sh);
    }

    [Test]
    public void Constructor_ShouldAssignAUniqueSessionId()
    {
        var stream1 = new MultiplexedStream(new MemoryStream(), multiplexed: false);
        var stream2 = new MultiplexedStream(new MemoryStream(), multiplexed: false);

        var sut1 = new DockerShellSession(Guid.NewGuid(), ShellType.Bash, stream1);
        var sut2 = new DockerShellSession(Guid.NewGuid(), ShellType.Bash, stream2);

        sut1.SessionId.ShouldNotBe(Guid.Empty);
        sut1.SessionId.ShouldNotBe(sut2.SessionId);
    }

    [Test]
    public async Task ReadAsync_ShouldReturnWrittenBytes_WhenStreamIsNotMultiplexed()
    {
        var underlying = new MemoryStream("hello"u8.ToArray());
        var stream = new MultiplexedStream(underlying, multiplexed: false);
        var sut = new DockerShellSession(Guid.NewGuid(), ShellType.Bash, stream);

        var buffer = new byte[16];
        var result = await sut.ReadAsync(buffer, CancellationToken.None);

        result.Count.ShouldBe(5);
        buffer[..5].ShouldBe("hello"u8.ToArray());
        result.Eof.ShouldBeFalse();
    }

    [Test]
    public async Task ReadAsync_ShouldReportEof_WhenUnderlyingStreamIsExhausted()
    {
        var underlying = new MemoryStream();
        var stream = new MultiplexedStream(underlying, multiplexed: false);
        var sut = new DockerShellSession(Guid.NewGuid(), ShellType.Bash, stream);

        var buffer = new byte[16];
        var result = await sut.ReadAsync(buffer, CancellationToken.None);

        result.Count.ShouldBe(0);
        result.Eof.ShouldBeTrue();
    }

    [Test]
    public async Task WriteAsync_ShouldWriteBytesToTheUnderlyingStream()
    {
        var underlying = new MemoryStream();
        var stream = new MultiplexedStream(underlying, multiplexed: false);
        var sut = new DockerShellSession(Guid.NewGuid(), ShellType.Bash, stream);

        await sut.WriteAsync("echo hi"u8.ToArray(), CancellationToken.None);

        underlying.ToArray().ShouldBe("echo hi"u8.ToArray());
    }

    [Test]
    public async Task DisposeAsync_ShouldDisposeTheUnderlyingStream()
    {
        var underlying = new MemoryStream();
        var stream = new MultiplexedStream(underlying, multiplexed: false);
        var sut = new DockerShellSession(Guid.NewGuid(), ShellType.Bash, stream);

        await sut.DisposeAsync();

        Should.Throw<ObjectDisposedException>(() => underlying.WriteByte(1));
    }
}
