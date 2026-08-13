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
    public void Apply_UpdatesAndRenormalizesHostname()
    {
        var domain = ServiceRegistryDomain.Create(Guid.NewGuid(), "example.com", 80);

        domain.Apply("New.Example.COM", Optional<int>.None);

        domain.Hostname.ShouldBe("new.example.com");
    }

    [Test]
    public void Reconstitute_SetsAllFields()
    {
        var id = Guid.NewGuid();
        var entryId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var domain = ServiceRegistryDomain.Reconstitute(id, entryId, "example.com", 8080, now, now);

        domain.Id.ShouldBe(id);
        domain.ServiceRegistryEntryId.ShouldBe(entryId);
        domain.Hostname.ShouldBe("example.com");
        domain.ContainerPort.ShouldBe(8080);
        domain.CreatedAt.ShouldBe(now);
        domain.UpdatedAt.ShouldBe(now);
    }
}