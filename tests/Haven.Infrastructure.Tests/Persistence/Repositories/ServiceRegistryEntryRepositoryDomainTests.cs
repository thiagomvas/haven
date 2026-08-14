using Haven.Domain.Aggregates;
using Haven.Domain.Enums;
using Haven.Infrastructure.Persistence;
using Haven.Infrastructure.Persistence.Repositories;
using Haven.Testing.Common;

using Microsoft.EntityFrameworkCore;

using Shouldly;

namespace Haven.Infrastructure.Tests.Persistence.Repositories;

[Category("Unit")]
public sealed class ServiceRegistryEntryRepositoryDomainTests
{
    private HavenDbContext _context = null!;
    private ServiceRegistryEntryRepository _sut = null!;

    [SetUp]
    public void Setup()
    {
        _context = TestDbContextFactory.CreateUnitDbContext();
        _sut = new ServiceRegistryEntryRepository(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    private async Task<Guid> CreateServiceAsync(string name)
    {
        var project = Project.Create($"project-{Guid.NewGuid()}");
        var environment = project.AddEnvironment("production", "prod");
        var service = environment.AddService(name, ServiceType.DockerImage, ExposureMode.None);

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        return service.Id;
    }

    [Test]
    public async Task HostnameExistsAsync_WhenNoDomainWithHostname_ReturnsFalse()
    {
        var exists = await _sut.HostnameExistsAsync("example.com", excludingDomainId: null, CancellationToken.None);

        exists.ShouldBeFalse();
    }

    [Test]
    public async Task HostnameExistsAsync_WhenDomainWithHostnameExists_ReturnsTrue()
    {
        var serviceId = await CreateServiceAsync("svc-a");
        var entry = ServiceRegistryEntry.Create(serviceId);
        entry.AddDomain("example.com", 8080);
        await _sut.InsertAsync(entry, CancellationToken.None);
        await _context.SaveChangesAsync();

        var exists = await _sut.HostnameExistsAsync("example.com", excludingDomainId: null, CancellationToken.None);

        exists.ShouldBeTrue();
    }

    [Test]
    public async Task HostnameExistsAsync_ExcludingOwnDomainId_ReturnsFalse()
    {
        var serviceId = await CreateServiceAsync("svc-a");
        var entry = ServiceRegistryEntry.Create(serviceId);
        var domain = entry.AddDomain("example.com", 8080);
        await _sut.InsertAsync(entry, CancellationToken.None);
        await _context.SaveChangesAsync();

        var exists = await _sut.HostnameExistsAsync("example.com", excludingDomainId: domain.Id, CancellationToken.None);

        exists.ShouldBeFalse();
    }

    [Test]
    public async Task UniqueIndex_RejectsDuplicateHostnameAcrossDifferentEntries()
    {
        var firstServiceId = await CreateServiceAsync("svc-a");
        var firstEntry = ServiceRegistryEntry.Create(firstServiceId);
        firstEntry.AddDomain("example.com", 8080);
        await _sut.InsertAsync(firstEntry, CancellationToken.None);
        await _context.SaveChangesAsync();

        var secondServiceId = await CreateServiceAsync("svc-b");
        var secondEntry = ServiceRegistryEntry.Create(secondServiceId);
        secondEntry.AddDomain("example.com", 3000);
        await _sut.InsertAsync(secondEntry, CancellationToken.None);

        await Should.ThrowAsync<DbUpdateException>(() => _context.SaveChangesAsync());
    }
}