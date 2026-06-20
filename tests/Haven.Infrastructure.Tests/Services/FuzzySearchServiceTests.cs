using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Infrastructure.Services;

using NSubstitute;

using Shouldly;

namespace Haven.Infrastructure.Tests.Services;

[Category("Unit")]
public sealed class FuzzySearchServiceTests
{
    private FuzzySearchService _sut = null!;
    private IFuzzySearchableRepository _repoA = null!;
    private IFuzzySearchableRepository _repoB = null!;

    [SetUp]
    public void Setup()
    {
        _repoA = Substitute.For<IFuzzySearchableRepository>();
        _repoB = Substitute.For<IFuzzySearchableRepository>();

        _repoA.FuzzySearchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns([]);
        _repoB.FuzzySearchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns([]);

        _sut = new FuzzySearchService([_repoA, _repoB]);
    }

    [Test]
    public async Task FuzzySearchAsync_WhenNoRepositoriesReturnResults_ShouldReturnEmpty()
    {
        var result = await _sut.FuzzySearchAsync("query");

        result.ShouldBeEmpty();
    }

    [Test]
    public async Task FuzzySearchAsync_WhenOneRepositoryReturnsResults_ShouldReturnThem()
    {
        _repoA.FuzzySearchAsync("nginx", Arg.Any<CancellationToken>())
            .Returns([Result("nginx-service", "nginx")]);

        var result = await _sut.FuzzySearchAsync("nginx");

        result.ShouldHaveSingleItem();
        result.Single().Label.ShouldBe("nginx-service");
    }

    [Test]
    public async Task FuzzySearchAsync_WhenMultipleRepositoriesReturnResults_ShouldCombineThem()
    {
        _repoA.FuzzySearchAsync("api", Arg.Any<CancellationToken>())
            .Returns([Result("api-service", "api-service")]);
        _repoB.FuzzySearchAsync("api", Arg.Any<CancellationToken>())
            .Returns([Result("api-project", "api-project")]);

        var result = await _sut.FuzzySearchAsync("api");

        result.Count().ShouldBe(2);
    }

    [Test]
    public async Task FuzzySearchAsync_ShouldOrderResultsByDescendingSimilarity()
    {
        _repoA.FuzzySearchAsync("nginx", Arg.Any<CancellationToken>())
            .Returns([
                Result("nginx", "nginx"),
                Result("nginx-proxy", "nginx-proxy"),
                Result("something-unrelated", "something-unrelated"),
            ]);

        var result = (await _sut.FuzzySearchAsync("nginx")).ToList();

        result.ShouldBeInOrder(SortDirection.Descending, Comparer<FuzzySearchResult>.Create((a, b) => a.Similarity.CompareTo(b.Similarity)));
    }

    [Test]
    public async Task FuzzySearchAsync_ShouldRecomputeSimilarityUsingPartialRatio()
    {
        _repoA.FuzzySearchAsync("nginx", Arg.Any<CancellationToken>())
            .Returns([Result("nginx", "nginx", similarity: 0)]);

        var result = await _sut.FuzzySearchAsync("nginx");

        result.Single().Similarity.ShouldBeGreaterThan(0);
    }

    [Test]
    public async Task FuzzySearchAsync_ShouldRespectCountLimit()
    {
        _repoA.FuzzySearchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Enumerable.Range(1, 20).Select(i => Result($"service-{i}", $"service-{i}")).ToList());

        var result = await _sut.FuzzySearchAsync("service", count: 5);

        result.Count().ShouldBe(5);
    }

    [Test]
    public async Task FuzzySearchAsync_WhenCountExceedsResults_ShouldReturnAllResults()
    {
        _repoA.FuzzySearchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([Result("only-one", "only-one")]);

        var result = await _sut.FuzzySearchAsync("only-one", count: 50);

        result.ShouldHaveSingleItem();
    }

    [Test]
    public async Task FuzzySearchAsync_ShouldPassCancellationTokenToRepositories()
    {
        using var cts = new CancellationTokenSource();

        await _sut.FuzzySearchAsync("query", cancellationToken: cts.Token);

        await _repoA.Received(1).FuzzySearchAsync("query", cts.Token);
        await _repoB.Received(1).FuzzySearchAsync("query", cts.Token);
    }

    [Test]
    public async Task FuzzySearchAsync_ShouldQueriesAllRepositoriesInParallel()
    {
        await _sut.FuzzySearchAsync("query");

        await _repoA.Received(1).FuzzySearchAsync("query", Arg.Any<CancellationToken>());
        await _repoB.Received(1).FuzzySearchAsync("query", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task FuzzySearchAsync_WhenNoRepositoriesRegistered_ShouldReturnEmpty()
    {
        _sut = new FuzzySearchService([]);

        var result = await _sut.FuzzySearchAsync("query");

        result.ShouldBeEmpty();
    }

    private static FuzzySearchResult Result(string label, string entityType, double similarity = 50) =>
        new(entityType, Guid.NewGuid(), label, similarity);
}