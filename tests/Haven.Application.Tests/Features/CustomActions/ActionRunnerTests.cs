using Haven.Application.Common;
using Haven.Application.Features.CustomActions.Abstractions;
using Haven.Application.Features.CustomActions.Services;
using Haven.Domain.Entities;
using Haven.Domain.ValueObjects;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

namespace Haven.Application.Tests.Features.CustomActions;

[Category("Unit")]
public sealed class ActionRunnerTests
{
    private ICustomActionRepository _repository = null!;

    [SetUp]
    public void Setup() => _repository = Substitute.For<ICustomActionRepository>();

    private ActionRunner CreateSut(params IActionStrategy[] strategies) =>
        new(strategies, _repository, NullLogger<ActionRunner>.Instance);

    private static IActionStrategy Strategy(bool canHandle, Result? result = null)
    {
        var strategy = Substitute.For<IActionStrategy>();
        strategy.CanHandle(Arg.Any<CustomAction>()).Returns(canHandle);
        strategy.ExecuteAsync(Arg.Any<CustomAction>(), Arg.Any<CancellationToken>())
            .Returns(result ?? Result.Success());
        return strategy;
    }

    [Test]
    public async Task RunActionAsync_ActionNotFound_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((CustomAction?)null);
        var strategy = Strategy(true);

        var result = await CreateSut(strategy).RunActionAsync(id, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("NOT_FOUND");
        await strategy.DidNotReceiveWithAnyArgs().ExecuteAsync(default!, default);
    }

    [Test]
    public async Task RunActionAsync_NoMatchingStrategy_ReturnsNotFound()
    {
        var action = CustomActionTestData.Create(CustomActionTestData.Http());
        _repository.GetByIdAsync(action.Id, Arg.Any<CancellationToken>()).Returns(action);
        var strategy = Strategy(false);

        var result = await CreateSut(strategy).RunActionAsync(action.Id, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Message.ShouldContain(nameof(IActionStrategy));
        await strategy.DidNotReceiveWithAnyArgs().ExecuteAsync(default!, default);
    }

    [Test]
    public async Task RunActionAsync_NoStrategiesRegistered_ReturnsNotFound()
    {
        var action = CustomActionTestData.Create(CustomActionTestData.Http());
        _repository.GetByIdAsync(action.Id, Arg.Any<CancellationToken>()).Returns(action);

        var result = await CreateSut().RunActionAsync(action.Id, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task RunActionAsync_UsesFirstMatchingStrategyOnly()
    {
        var action = CustomActionTestData.Create(CustomActionTestData.Http());
        _repository.GetByIdAsync(action.Id, Arg.Any<CancellationToken>()).Returns(action);
        var skipped = Strategy(false);
        var first = Strategy(true);
        var second = Strategy(true);

        var result = await CreateSut(skipped, first, second).RunActionAsync(action.Id, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await first.Received(1).ExecuteAsync(action, Arg.Any<CancellationToken>());
        await second.DidNotReceiveWithAnyArgs().ExecuteAsync(default!, default);
        await skipped.DidNotReceiveWithAnyArgs().ExecuteAsync(default!, default);
    }

    [Test]
    public async Task RunActionAsync_ReturnsStrategyResult()
    {
        var action = CustomActionTestData.Create(CustomActionTestData.Http());
        _repository.GetByIdAsync(action.Id, Arg.Any<CancellationToken>()).Returns(action);
        var strategy = Strategy(true, Result.Failure(Error.Failed));

        var result = await CreateSut(strategy).RunActionAsync(action.Id, CancellationToken.None);

        result.Error.ShouldBe(Error.Failed);
    }
}
