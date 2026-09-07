using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Infrastructure.Persistence;
using Haven.Infrastructure.Persistence.Repositories;
using Haven.Testing.Common;

using Shouldly;

namespace Haven.Infrastructure.Tests.Persistence.Repositories;

[Category("Unit")]
public sealed class NotificationChannelConfigRepositorySortingSearchTests
{
    private HavenDbContext _context = null!;
    private NotificationChannelConfigRepository _sut = null!;

    [SetUp]
    public void Setup()
    {
        _context = TestDbContextFactory.CreateUnitDbContext();
        _sut = new NotificationChannelConfigRepository(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    private async Task SeedAsync(params NotificationChannelConfig[] configs)
    {
        _context.NotificationChannelConfigs.AddRange(configs);
        await _context.SaveChangesAsync();
    }

    private static NotificationChannelConfig CreateConfig(
        string name, NotificationChannel channel = NotificationChannel.Webhook, bool enabled = true) =>
        NotificationChannelConfig.Create(name, channel, configJson: "{}", enabled);

    [Test]
    public async Task GetPagedAsync_NoSortBy_DefaultsToNameAscending()
    {
        await SeedAsync(
            CreateConfig("Charlie"),
            CreateConfig("alpha"),
            CreateConfig("Bravo"));

        var result = await _sut.GetPagedAsync(1, 10, cancellationToken: CancellationToken.None);

        result.Items.Select(c => c.Name).ShouldBe(["alpha", "Bravo", "Charlie"]);
    }

    [Test]
    public async Task GetPagedAsync_SortByNameIsCaseInsensitive()
    {
        // Regression test for the "Normalize names before sorting alphabetically" fix:
        // ordering by raw Name would put all-uppercase/lowercase names in separate blocks
        // instead of interleaving them alphabetically.
        await SeedAsync(
            CreateConfig("banana"),
            CreateConfig("Apple"),
            CreateConfig("cherry"));

        var result = await _sut.GetPagedAsync(1, 10, sortBy: "Name", cancellationToken: CancellationToken.None);

        result.Items.Select(c => c.Name).ShouldBe(["Apple", "banana", "cherry"]);
    }

    [Test]
    public async Task GetPagedAsync_SortByNameDescending_ReturnsReverseOrder()
    {
        await SeedAsync(
            CreateConfig("alpha"),
            CreateConfig("Bravo"),
            CreateConfig("Charlie"));

        var result = await _sut.GetPagedAsync(
            1, 10, sortBy: "Name", search: null, sortAscending: false, CancellationToken.None);

        result.Items.Select(c => c.Name).ShouldBe(["Charlie", "Bravo", "alpha"]);
    }

    [Test]
    public async Task GetPagedAsync_SortByChannel_GroupsByChannelThenNameAscending()
    {
        await SeedAsync(
            CreateConfig("zeta", NotificationChannel.Discord),
            CreateConfig("beta", NotificationChannel.Webhook),
            CreateConfig("alpha", NotificationChannel.Discord));

        var result = await _sut.GetPagedAsync(1, 10, sortBy: "Channel", cancellationToken: CancellationToken.None);

        result.Items.Select(c => (c.Channel, c.Name)).ShouldBe(
        [
            (NotificationChannel.Discord, "alpha"),
            (NotificationChannel.Discord, "zeta"),
            (NotificationChannel.Webhook, "beta"),
        ]);
    }

    [Test]
    public async Task GetPagedAsync_SortByChannelDescending_ReturnsChannelsInReverseOrder()
    {
        await SeedAsync(
            CreateConfig("alpha", NotificationChannel.Discord),
            CreateConfig("beta", NotificationChannel.Webhook));

        var result = await _sut.GetPagedAsync(
            1, 10, sortBy: "Channel", search: null, sortAscending: false, CancellationToken.None);

        result.Items.Select(c => c.Channel).ShouldBe([NotificationChannel.Webhook, NotificationChannel.Discord]);
    }

    [Test]
    public async Task GetPagedAsync_SortByEnabled_GroupsDisabledBeforeEnabledThenByName()
    {
        await SeedAsync(
            CreateConfig("zeta", enabled: true),
            CreateConfig("beta", enabled: false),
            CreateConfig("alpha", enabled: true));

        var result = await _sut.GetPagedAsync(1, 10, sortBy: "Enabled", cancellationToken: CancellationToken.None);

        result.Items.Select(c => (c.Enabled, c.Name)).ShouldBe(
        [
            (false, "beta"),
            (true, "alpha"),
            (true, "zeta"),
        ]);
    }

    [Test]
    public async Task GetPagedAsync_SortByUnknownValue_FallsBackToNameAscending()
    {
        await SeedAsync(
            CreateConfig("Charlie"),
            CreateConfig("alpha"));

        var result = await _sut.GetPagedAsync(1, 10, sortBy: "NotARealColumn", cancellationToken: CancellationToken.None);

        result.Items.Select(c => c.Name).ShouldBe(["alpha", "Charlie"]);
    }

    [TestCase("Name")]
    [TestCase("name")]
    [TestCase("NAME")]
    public async Task GetPagedAsync_SortByIsCaseInsensitive(string sortBy)
    {
        await SeedAsync(CreateConfig("Charlie"), CreateConfig("alpha"));

        var result = await _sut.GetPagedAsync(1, 10, sortBy: sortBy, cancellationToken: CancellationToken.None);

        result.Items.Select(c => c.Name).ShouldBe(["alpha", "Charlie"]);
    }

    [Test]
    public async Task GetPagedAsync_SearchMatchesNameCaseInsensitivePartial()
    {
        await SeedAsync(
            CreateConfig("Production Discord"),
            CreateConfig("Staging Webhook"),
            CreateConfig("Backup Ntfy"));

        var result = await _sut.GetPagedAsync(1, 10, search: "disc", cancellationToken: CancellationToken.None);

        result.Items.Select(c => c.Name).ShouldBe(["Production Discord"]);
    }

    [Test]
    public async Task GetPagedAsync_SearchMatchesChannelCaseInsensitivePartial()
    {
        await SeedAsync(
            CreateConfig("Primary", NotificationChannel.Discord),
            CreateConfig("Secondary", NotificationChannel.Webhook));

        var result = await _sut.GetPagedAsync(1, 10, search: "webhook", cancellationToken: CancellationToken.None);

        result.Items.Select(c => c.Name).ShouldBe(["Secondary"]);
    }

    [Test]
    public async Task GetPagedAsync_SearchWithNoMatches_ReturnsEmptyResult()
    {
        await SeedAsync(CreateConfig("Production Discord"));

        var result = await _sut.GetPagedAsync(1, 10, search: "nonexistent", cancellationToken: CancellationToken.None);

        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
    }

    [Test]
    public async Task GetPagedAsync_SearchIsCombinedWithSort()
    {
        await SeedAsync(
            CreateConfig("Zulu Discord"),
            CreateConfig("Alpha Discord"),
            CreateConfig("Alpha Webhook"));

        var result = await _sut.GetPagedAsync(
            1, 10, sortBy: "Name", search: "discord", sortAscending: true, CancellationToken.None);

        result.Items.Select(c => c.Name).ShouldBe(["Alpha Discord", "Zulu Discord"]);
    }

    [Test]
    public async Task GetPagedAsync_BlankSearch_IsTreatedAsNoFilter()
    {
        await SeedAsync(CreateConfig("Alpha"), CreateConfig("Beta"));

        var result = await _sut.GetPagedAsync(1, 10, search: "   ", cancellationToken: CancellationToken.None);

        result.TotalCount.ShouldBe(2);
    }

    [Test]
    public async Task GetPagedAsync_RespectsPageSizeAndTotalCountAcrossSortedResults()
    {
        await SeedAsync(
            CreateConfig("Alpha"),
            CreateConfig("Bravo"),
            CreateConfig("Charlie"));

        var firstPage = await _sut.GetPagedAsync(1, 2, sortBy: "Name", cancellationToken: CancellationToken.None);
        var secondPage = await _sut.GetPagedAsync(2, 2, sortBy: "Name", cancellationToken: CancellationToken.None);

        firstPage.Items.Select(c => c.Name).ShouldBe(["Alpha", "Bravo"]);
        firstPage.TotalCount.ShouldBe(3);
        secondPage.Items.Select(c => c.Name).ShouldBe(["Charlie"]);
    }
}
