using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Exceptions;

using Shouldly;

namespace Haven.Domain.Tests.Entities;

[TestFixture]
[Category("Unit")]
public sealed class ServiceRegistryDomainTests
{
    [Test]
    public void Create_NormalizesHostnameToLowercaseAndTrims()
    {
        var entryId = Guid.NewGuid();

        var domain = ServiceRegistryDomain.Create(entryId, "  App.Example.COM  ", 8080);

        domain.ServiceRegistryEntryId.ShouldBe(entryId);
        domain.Hostname.ShouldBe("app.example.com");
        domain.ContainerPort.ShouldBe(8080);
    }

    [Test]
    public void Create_EmptyHostname_Throws()
    {
        Should.Throw<ValidationException>(() =>
            ServiceRegistryDomain.Create(Guid.NewGuid(), "   ", 8080));
    }

    [TestCase("not a hostname!!")]
    [TestCase("http://example.com")]
    public void Create_InvalidHostname_Throws(string hostname)
    {
        Should.Throw<ValidationException>(() =>
            ServiceRegistryDomain.Create(Guid.NewGuid(), hostname, 8080));
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(65536)]
    public void Create_PortOutOfRange_Throws(int port)
    {
        Should.Throw<ValidationException>(() =>
            ServiceRegistryDomain.Create(Guid.NewGuid(), "example.com", port));
    }

    [Test]
    public void Create_DefaultsEnableTlsToFalse()
    {
        var domain = ServiceRegistryDomain.Create(Guid.NewGuid(), "example.com", 80);

        domain.EnableTls.ShouldBeFalse();
    }

    [Test]
    public void Create_EnableTlsTrue_SetsEnableTls()
    {
        var domain = ServiceRegistryDomain.Create(Guid.NewGuid(), "example.com", 80, enableTls: true);

        domain.EnableTls.ShouldBeTrue();
    }

    [Test]
    public void Apply_UpdatesAndRenormalizesHostname()
    {
        var domain = ServiceRegistryDomain.Create(Guid.NewGuid(), "example.com", 80);

        domain.Apply("New.Example.COM", Optional<int>.None);

        domain.Hostname.ShouldBe("new.example.com");
    }

    [Test]
    public void Apply_EnableTlsProvided_UpdatesEnableTls()
    {
        var domain = ServiceRegistryDomain.Create(Guid.NewGuid(), "example.com", 80);

        domain.Apply(Optional<string>.None, Optional<int>.None, true);

        domain.EnableTls.ShouldBeTrue();
    }

    [Test]
    public void Apply_EnableTlsNotProvided_LeavesEnableTlsUnchanged()
    {
        var domain = ServiceRegistryDomain.Create(Guid.NewGuid(), "example.com", 80, enableTls: true);

        domain.Apply("new.example.com", Optional<int>.None);

        domain.EnableTls.ShouldBeTrue();
    }

    [Test]
    public void Reconstitute_SetsAllFields()
    {
        var id = Guid.NewGuid();
        var entryId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var domain = ServiceRegistryDomain.Reconstitute(id, entryId, "example.com", 8080, true, now, now);

        domain.Id.ShouldBe(id);
        domain.ServiceRegistryEntryId.ShouldBe(entryId);
        domain.Hostname.ShouldBe("example.com");
        domain.ContainerPort.ShouldBe(8080);
        domain.EnableTls.ShouldBeTrue();
        domain.CreatedAt.ShouldBe(now);
        domain.UpdatedAt.ShouldBe(now);
    }
}