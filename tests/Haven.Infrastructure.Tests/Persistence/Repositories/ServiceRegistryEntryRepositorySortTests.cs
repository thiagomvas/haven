using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;
using Haven.Infrastructure.Persistence;
using Haven.Infrastructure.Persistence.Repositories;
using Haven.Testing.Common;

using Microsoft.EntityFrameworkCore;

using Shouldly;

namespace Haven.Infrastructure.Tests.Persistence.Repositories;

[Category("Unit")]
public sealed class ServiceRegistryEntryRepositorySortTests
{
    private HavenDbContext _context = null!;
    private ServiceRegistryEntryRepository _sut = null!;

    [SetUp]
    public async Task Setup()
    {
        _context = TestDbContextFactory.CreateUnitDbContext();
        _sut = new ServiceRegistryEntryRepository(_context);

        var project = Project.Create("sort-project");
        var environment = project.AddEnvironment("production", "prod");
        var services = new[]
        {
            environment.AddService("svc-a", ServiceType.DockerImage, ExposureMode.None),
            environment.AddService("svc-b", ServiceType.DockerImage, ExposureMode.External),
            environment.AddService("svc-c", ServiceType.DockerImage, ExposureMode.Internal),
        };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var names = new[] { "bravo", "alpha", "charlie" };
        for (var i = 0; i < services.Length; i++)
        {
            var entry = ServiceRegistryEntry.Create(services[i].Id);
            entry.ContainerName = names[i];
            entry.ApplyRuntimeState(i == 0 ? ServiceRuntimeState.Running : i == 1 ? ServiceRuntimeState.Stopped : ServiceRuntimeState.Restarting, DateTime.UtcNow);
            if (i == 1) entry.AddDomain("alpha.example.com", 8080);
            await _sut.InsertAsync(entry, CancellationToken.None);
        }
        await _context.SaveChangesAsync();
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private async Task<string?[]> NamesAsync(string? sortBy, bool descending)
    {
        var result = await _sut.GetPagedAsync(1, 10, null, sortBy, descending, CancellationToken.None);
        return result.Items.Select(e => e.ContainerName).ToArray();
    }

    [Test]
    public async Task GetPagedAsync_SortByNameAscending_OrdersByContainerName() =>
        (await NamesAsync("name", false)).ShouldBe(["alpha", "bravo", "charlie"]);

    [Test]
    public async Task GetPagedAsync_SortByNameDescending_ReversesOrder() =>
        (await NamesAsync("name", true)).ShouldBe(["charlie", "bravo", "alpha"]);

    [Test]
    public async Task GetPagedAsync_SortByExposure_OrdersByServiceExposureMode() =>
        // bravo=None, alpha=External, charlie=Internal -> None < Internal < External
        (await NamesAsync("exposure", false)).ShouldBe(["bravo", "charlie", "alpha"]);

    // Regression: RuntimeState is persisted as text, so sorting must not cast it to int
    // (Postgres: 22P02 invalid input syntax for type integer: "Stopped"). The expected order
    // differs from the name tiebreaker order, so a cast-to-0 on SQLite would also fail these.
    [Test]
    public async Task GetPagedAsync_SortByStatusAscending_OrdersByStoredStateText() =>
        // charlie=Restarting, bravo=Running, alpha=Stopped
        (await NamesAsync("status", false)).ShouldBe(["charlie", "bravo", "alpha"]);

    [Test]
    public async Task GetPagedAsync_SortByStatusDescending_ReversesOrder() =>
        (await NamesAsync("status", true)).ShouldBe(["alpha", "bravo", "charlie"]);

    [Test]
    public async Task GetPagedAsync_SortByStatus_TranslatesWithoutIntegerCast()
    {
        var sql = _context.ServiceRegistryEntries
            .OrderBy(e => e.RuntimeState)
            .ToQueryString();

        sql.ShouldNotContain("CAST(");
    }

    [Test]
    public async Task GetPagedAsync_FilterByStatus_ReturnsOnlyMatching()
    {
        var result = await _sut.GetPagedAsync(1, 10, new ServiceRegistryEntryFilter(Status: RegistryStatusFilter.Starting), null, false, CancellationToken.None);

        result.Items.Select(e => e.ContainerName).ShouldBe(["charlie"]);
    }

    [Test]
    public async Task GetPagedAsync_FilterByUnknownStatus_ExcludesStartingEntries()
    {
        var result = await _sut.GetPagedAsync(1, 10, new ServiceRegistryEntryFilter(Status: RegistryStatusFilter.Unknown), null, false, CancellationToken.None);

        result.TotalCount.ShouldBe(0);
    }

    [Test]
    public async Task GetPagedAsync_FilterByExposure_ReturnsOnlyMatching()
    {
        var result = await _sut.GetPagedAsync(1, 10, new ServiceRegistryEntryFilter(Exposure: ExposureMode.External), null, false, CancellationToken.None);

        result.Items.Select(e => e.ContainerName).ShouldBe(["alpha"]);
    }

    [Test]
    public async Task GetPagedAsync_SearchByProjectName_MatchesAll()
    {
        var result = await _sut.GetPagedAsync(1, 10, new ServiceRegistryEntryFilter(Search: "sort-project"), null, false, CancellationToken.None);

        result.TotalCount.ShouldBe(3);
    }

    [Test]
    public async Task GetPagedAsync_SearchByDomainHostname_MatchesOwningEntry()
    {
        var result = await _sut.GetPagedAsync(1, 10, new ServiceRegistryEntryFilter(Search: "alpha.example"), null, false, CancellationToken.None);

        result.Items.Select(e => e.ContainerName).ShouldBe(["alpha"]);
    }

    [Test]
    public async Task GetSummaryAsync_CountsByStatusAndDomains()
    {
        var summary = await _sut.GetSummaryAsync(CancellationToken.None);

        summary.ShouldBe(new ServiceRegistrySummary(Total: 3, Running: 1, Stopped: 1, Starting: 1, Unknown: 0, DomainCount: 1, ExposedExternally: 1));
    }

    [Test]
    public async Task GetDomainsPagedAsync_LoadsOwnerAndFiltersBySearch()
    {
        var result = await _sut.GetDomainsPagedAsync(1, 10, "alpha", null, false, CancellationToken.None);

        result.Items.Count.ShouldBe(1);
        result.Items[0].ServiceRegistryEntry!.Service!.Environment!.Project!.Name.ShouldBe("sort-project");
    }

    [Test]
    public async Task GetPagedAsync_UnknownSortKey_DoesNotThrow() =>
        (await NamesAsync("nonsense", false)).Length.ShouldBe(3);
}