using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Exceptions;

using Shouldly;

namespace Haven.Domain.Tests.Aggregates;

[TestFixture]
[Category("Unit")]
public sealed class ServiceRegistryEntryDomainManagementTests
{
    private static ServiceRegistryEntry NewEntry() => ServiceRegistryEntry.Create(Guid.NewGuid());

    [Test]
    public void AddDomain_AddsToEntry()
    {
        var entry = NewEntry();

        var domain = entry.AddDomain("example.com", 8080);

        entry.Domains.Count.ShouldBe(1);
        entry.Domains.First().ShouldBe(domain);
        domain.ServiceRegistryEntryId.ShouldBe(entry.Id);
    }

    [Test]
    public void AddDomain_MultipleDomains_AddsEach()
    {
        var entry = NewEntry();

        entry.AddDomain("example.com", 8080);
        entry.AddDomain("api.example.com", 3000);

        entry.Domains.Count.ShouldBe(2);
    }

    [Test]
    public void AddDomain_DuplicateHostnameWithinEntry_Throws()
    {
        var entry = NewEntry();
        entry.AddDomain("example.com", 8080);

        Should.Throw<ValidationException>(() => entry.AddDomain("EXAMPLE.com", 3000));

        entry.Domains.Count.ShouldBe(1);
    }

    [Test]
    public void UpdateDomain_ChangesFields()
    {
        var entry = NewEntry();
        var domain = entry.AddDomain("example.com", 8080);

        entry.UpdateDomain(domain, "new.example.com", 3000);

        domain.Hostname.ShouldBe("new.example.com");
        domain.ContainerPort.ShouldBe(3000);
    }

    [Test]
    public void UpdateDomain_ToHostnameCollidingWithSiblingDomain_Throws()
    {
        var entry = NewEntry();
        entry.AddDomain("example.com", 8080);
        var api = entry.AddDomain("api.example.com", 3000);

        Should.Throw<ValidationException>(() => entry.UpdateDomain(api, "example.com", default));

        api.Hostname.ShouldBe("api.example.com");
    }

    [Test]
    public void UpdateDomain_HostnameUnchangedCase_DoesNotThrow()
    {
        var entry = NewEntry();
        var domain = entry.AddDomain("example.com", 8080);

        Should.NotThrow(() => entry.UpdateDomain(domain, "EXAMPLE.com", 9090));

        domain.ContainerPort.ShouldBe(9090);
    }

    [Test]
    public void UpdateDomain_ForeignDomain_Throws()
    {
        var entry = NewEntry();
        var foreign = ServiceRegistryDomain.Create(Guid.NewGuid(), "example.com", 8080);

        Should.Throw<ValidationException>(() => entry.UpdateDomain(foreign, "x.com", default));
    }

    [Test]
    public void RemoveDomain_RemovesFromEntry()
    {
        var entry = NewEntry();
        var domain = entry.AddDomain("example.com", 8080);

        entry.RemoveDomain(domain);

        entry.Domains.ShouldBeEmpty();
    }
}
