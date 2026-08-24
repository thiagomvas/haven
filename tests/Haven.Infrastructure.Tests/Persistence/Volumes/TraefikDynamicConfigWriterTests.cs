using Haven.Application.Configuration;
using Haven.Infrastructure.Persistence.Volumes;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Haven.Infrastructure.Tests.Persistence.Volumes;

[TestFixture]
[Category("Unit")]
public class TraefikDynamicConfigWriterTests
{
    private string _root = null!;
    private TraefikDynamicConfigWriter _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), $"haven-traefik-dynamic-tests-{Guid.NewGuid()}");

        var options = Substitute.For<IOptionsMonitor<TraefikOptions>>();
        options.CurrentValue.Returns(new TraefikOptions { DynamicConfigRootPath = _root });

        var logger = Substitute.For<ILogger<TraefikDynamicConfigWriter>>();
        _sut = new TraefikDynamicConfigWriter(options, logger);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [Test]
    public async Task WriteDomainCertificateAsync_WritesCertKeyAndConfigFiles()
    {
        var domainId = Guid.NewGuid();

        var result = await _sut.WriteDomainCertificateAsync(domainId, "cert-content", "key-content", CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var domainDir = Path.Combine(_root, domainId.ToString());
        (await File.ReadAllTextAsync(Path.Combine(domainDir, "cert.pem"))).ShouldBe("cert-content");
        (await File.ReadAllTextAsync(Path.Combine(domainDir, "key.pem"))).ShouldBe("key-content");
        var config = await File.ReadAllTextAsync(Path.Combine(domainDir, "config.yml"));
        config.ShouldContain($"/etc/traefik/dynamic/{domainId}/cert.pem");
        config.ShouldContain($"/etc/traefik/dynamic/{domainId}/key.pem");
    }

    [Test]
    public async Task WriteDomainCertificateAsync_CalledTwice_OverwritesInPlace()
    {
        var domainId = Guid.NewGuid();
        await _sut.WriteDomainCertificateAsync(domainId, "cert-v1", "key-v1", CancellationToken.None);

        await _sut.WriteDomainCertificateAsync(domainId, "cert-v2", "key-v2", CancellationToken.None);

        var domainDir = Path.Combine(_root, domainId.ToString());
        (await File.ReadAllTextAsync(Path.Combine(domainDir, "cert.pem"))).ShouldBe("cert-v2");
    }

    [Test]
    public async Task RemoveDomainCertificateAsync_DeletesDomainDirectory()
    {
        var domainId = Guid.NewGuid();
        await _sut.WriteDomainCertificateAsync(domainId, "cert-content", "key-content", CancellationToken.None);

        var result = await _sut.RemoveDomainCertificateAsync(domainId, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        Directory.Exists(Path.Combine(_root, domainId.ToString())).ShouldBeFalse();
    }

    [Test]
    public async Task RemoveDomainCertificateAsync_NothingWritten_IsNoOp()
    {
        var result = await _sut.RemoveDomainCertificateAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public async Task WriteInternalApiRouterAsync_WritesStaticRouterConfig()
    {
        var result = await _sut.WriteInternalApiRouterAsync(CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var configPath = Path.Combine(_root, "_haven-internal", "config.yml");
        var config = await File.ReadAllTextAsync(configPath);
        config.ShouldContain("api@internal");
        config.ShouldContain("havenapi");
    }
}
