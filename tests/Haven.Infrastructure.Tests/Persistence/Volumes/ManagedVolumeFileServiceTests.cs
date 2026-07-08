using Haven.Application.Configuration;
using Haven.Infrastructure.Persistence.Volumes;
using Haven.Infrastructure.Utils;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Haven.Infrastructure.Tests.Persistence.Volumes;

[TestFixture]
[Category("Unit")]
public class ManagedVolumeFileServiceTests
{
    private string _root = null!;
    private Guid _serviceId;
    private Guid _volumeId;
    private ManagedVolumeFileService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), $"haven-managed-volume-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_root);

        _serviceId = Guid.NewGuid();
        _volumeId = Guid.NewGuid();

        var options = Substitute.For<IOptionsMonitor<VolumesOptions>>();
        options.CurrentValue.Returns(new VolumesOptions { RootPath = _root });

        var logger = Substitute.For<ILogger<ManagedVolumeFileService>>();
        _sut = new ManagedVolumeFileService(options, logger);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    private string VolumeRoot => DockerUtils.ManagedVolumeHostPath(_root, _serviceId, _volumeId);

    [Test]
    public async Task WriteAndReadFileAsync_WithValidRelativePath_RoundTrips()
    {
        var result = await _sut.WriteFileAsync(_serviceId, _volumeId, "config.txt", "hello world");
        result.IsSuccess.ShouldBeTrue();

        var read = await _sut.ReadFileAsync(_serviceId, _volumeId, "config.txt");
        read.IsSuccess.ShouldBeTrue();
        read.Value.ShouldBe("hello world");
    }

    [Test]
    public async Task WriteFileAsync_WithParentTraversal_IsRejected()
    {
        var result = await _sut.WriteFileAsync(_serviceId, _volumeId, "../../escape.txt", "pwned");

        result.IsFailure.ShouldBeTrue();
        File.Exists(Path.Combine(Path.GetDirectoryName(VolumeRoot)!, "..", "escape.txt")).ShouldBeFalse();
    }

    [Test]
    public async Task ReadFileAsync_ViaSymlinkEscapingVolumeRoot_IsRejected()
    {
        Directory.CreateDirectory(VolumeRoot);

        var outsideDir = Path.Combine(Path.GetTempPath(), $"haven-outside-{Guid.NewGuid()}");
        Directory.CreateDirectory(outsideDir);
        var secretPath = Path.Combine(outsideDir, "secret.txt");
        await File.WriteAllTextAsync(secretPath, "top secret");

        var symlinkPath = Path.Combine(VolumeRoot, "escape-link");
        File.CreateSymbolicLink(symlinkPath, secretPath);

        try
        {
            var result = await _sut.ReadFileAsync(_serviceId, _volumeId, "escape-link");
            result.IsFailure.ShouldBeTrue();
        }
        finally
        {
            Directory.Delete(outsideDir, recursive: true);
        }
    }

    [Test]
    public async Task WriteFileAsync_ViaSymlinkEscapingVolumeRoot_IsRejected()
    {
        Directory.CreateDirectory(VolumeRoot);

        var outsideDir = Path.Combine(Path.GetTempPath(), $"haven-outside-{Guid.NewGuid()}");
        Directory.CreateDirectory(outsideDir);

        var symlinkPath = Path.Combine(VolumeRoot, "escape-link");
        File.CreateSymbolicLink(symlinkPath, outsideDir);

        try
        {
            var result = await _sut.WriteFileAsync(_serviceId, _volumeId, "escape-link/payload.txt", "pwned");
            result.IsFailure.ShouldBeTrue();
            File.Exists(Path.Combine(outsideDir, "payload.txt")).ShouldBeFalse();
        }
        finally
        {
            Directory.Delete(outsideDir, recursive: true);
        }
    }

    [Test]
    public async Task DeleteFileAsync_ViaSymlinkEscapingVolumeRoot_IsRejected()
    {
        Directory.CreateDirectory(VolumeRoot);

        var outsideDir = Path.Combine(Path.GetTempPath(), $"haven-outside-{Guid.NewGuid()}");
        Directory.CreateDirectory(outsideDir);
        var secretPath = Path.Combine(outsideDir, "secret.txt");
        await File.WriteAllTextAsync(secretPath, "top secret");

        var symlinkPath = Path.Combine(VolumeRoot, "escape-link");
        File.CreateSymbolicLink(symlinkPath, secretPath);

        try
        {
            var result = await _sut.DeleteFileAsync(_serviceId, _volumeId, "escape-link");
            result.IsFailure.ShouldBeTrue();
            File.Exists(secretPath).ShouldBeTrue();
        }
        finally
        {
            Directory.Delete(outsideDir, recursive: true);
        }
    }

    [Test]
    public async Task ReadFileAsync_ViaSymlinkWithinVolumeRoot_IsAllowed()
    {
        Directory.CreateDirectory(VolumeRoot);
        var realPath = Path.Combine(VolumeRoot, "real.txt");
        await File.WriteAllTextAsync(realPath, "inside root");

        var symlinkPath = Path.Combine(VolumeRoot, "alias.txt");
        File.CreateSymbolicLink(symlinkPath, realPath);

        var result = await _sut.ReadFileAsync(_serviceId, _volumeId, "alias.txt");

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("inside root");
    }
}
