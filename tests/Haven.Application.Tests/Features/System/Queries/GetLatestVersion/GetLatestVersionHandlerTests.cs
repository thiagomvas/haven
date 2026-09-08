using Haven.Application.Common;
using Haven.Application.Common.Contracts;
using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Features.System.Queries.GetLatestVersion;

using NSubstitute;

using Shouldly;

using Version = Haven.Domain.ValueObjects.Version;

namespace Haven.Application.Tests.Features.System.Queries.GetLatestVersion;

[Category("Unit")]
public sealed class GetLatestVersionHandlerTests
{
    private IHavenVersionService _havenVersionService;
    private GetLatestVersionHandler _handler;

    [SetUp]
    public void Setup()
    {
        _havenVersionService = Substitute.For<IHavenVersionService>();
        _handler = new GetLatestVersionHandler(_havenVersionService);
    }

    [Test]
    public async Task Handler_ShouldReturnFailure_WhenGetLatestReleaseFails()
    {
        _havenVersionService.GetLatestReleaseAsync(forceRefresh: false, ct: Arg.Any<CancellationToken>())
            .Returns(Result<HavenReleaseInfo>.Failure(Error.NotFound));

        var result = await _handler.Handle(new GetLatestVersionQuery(), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(Error.NotFound);
    }

    [Test]
    public async Task Handler_ShouldReturnIsUpdateAvailableTrue_WhenLatestVersionIsGreaterThanCurrent()
    {
        _havenVersionService.CurrentVersion.Returns(Version.Create(1, 0, 0));
        _havenVersionService.GetLatestReleaseAsync(forceRefresh: false, ct: Arg.Any<CancellationToken>())
            .Returns(Result<HavenReleaseInfo>.Success(new HavenReleaseInfo(
                Version.Create(1, 1, 0), "v1.1.0", "https://github.com/example/haven/releases/tag/v1.1.0",
                "## Changelog\n- Added things", false, DateTimeOffset.UtcNow)));

        var result = await _handler.Handle(new GetLatestVersionQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.IsUpdateAvailable.ShouldBeTrue();
        result.Value.CurrentVersion.ShouldBe("1.0.0");
        result.Value.LatestVersion.ShouldBe("1.1.0");
    }

    [Test]
    public async Task Handler_ShouldReturnIsUpdateAvailableFalse_WhenLatestVersionEqualsCurrent()
    {
        _havenVersionService.CurrentVersion.Returns(Version.Create(1, 0, 0));
        _havenVersionService.GetLatestReleaseAsync(forceRefresh: false, ct: Arg.Any<CancellationToken>())
            .Returns(Result<HavenReleaseInfo>.Success(new HavenReleaseInfo(
                Version.Create(1, 0, 0), "v1.0.0", "https://github.com/example/haven/releases/tag/v1.0.0",
                "Initial release", false, DateTimeOffset.UtcNow)));

        var result = await _handler.Handle(new GetLatestVersionQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.IsUpdateAvailable.ShouldBeFalse();
    }

    [Test]
    public async Task Handler_ShouldReturnIsUpdateAvailableFalse_WhenLatestVersionIsLowerThanCurrent()
    {
        _havenVersionService.CurrentVersion.Returns(Version.Create(2, 0, 0));
        _havenVersionService.GetLatestReleaseAsync(forceRefresh: false, ct: Arg.Any<CancellationToken>())
            .Returns(Result<HavenReleaseInfo>.Success(new HavenReleaseInfo(
                Version.Create(1, 9, 0), "v1.9.0", "https://github.com/example/haven/releases/tag/v1.9.0",
                "Old release", false, DateTimeOffset.UtcNow)));

        var result = await _handler.Handle(new GetLatestVersionQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.IsUpdateAvailable.ShouldBeFalse();
    }

    [Test]
    public async Task Handler_ShouldMapAllReleaseFieldsOntoDto()
    {
        var publishedAt = new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero);
        _havenVersionService.CurrentVersion.Returns(Version.Create(1, 0, 0));
        _havenVersionService.GetLatestReleaseAsync(forceRefresh: false, ct: Arg.Any<CancellationToken>())
            .Returns(Result<HavenReleaseInfo>.Success(new HavenReleaseInfo(
                Version.Create(1, 2, 3, "beta.1"), "Haven 1.2.3-beta.1",
                "https://github.com/example/haven/releases/tag/v1.2.3-beta.1",
                "## What's new\n- Feature A\n- Fix B", true, publishedAt)));

        var result = await _handler.Handle(new GetLatestVersionQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.LatestVersion.ShouldBe("1.2.3-beta.1");
        result.Value.Name.ShouldBe("Haven 1.2.3-beta.1");
        result.Value.HtmlUrl.ShouldBe("https://github.com/example/haven/releases/tag/v1.2.3-beta.1");
        result.Value.Body.ShouldBe("## What's new\n- Feature A\n- Fix B");
        result.Value.Prerelease.ShouldBeTrue();
        result.Value.PublishedAt.ShouldBe(publishedAt);
    }

    [Test]
    public async Task Handler_ShouldNotForceRefresh_WhenFetchingLatestRelease()
    {
        _havenVersionService.CurrentVersion.Returns(Version.Create(1, 0, 0));
        _havenVersionService.GetLatestReleaseAsync(forceRefresh: false, ct: Arg.Any<CancellationToken>())
            .Returns(Result<HavenReleaseInfo>.Success(new HavenReleaseInfo(
                Version.Create(1, 0, 0), null, null, null, false, null)));

        await _handler.Handle(new GetLatestVersionQuery(), CancellationToken.None);

        await _havenVersionService.Received(1)
            .GetLatestReleaseAsync(forceRefresh: false, ct: Arg.Any<CancellationToken>());
    }
}
