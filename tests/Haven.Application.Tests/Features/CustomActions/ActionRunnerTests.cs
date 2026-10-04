using Haven.Application.Common;
using Haven.Application.Common.Templating;
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
    private IServiceTemplateNamespaceProvider _namespaces = null!;

    [SetUp]
    public void Setup()
    {
        _repository = Substitute.For<ICustomActionRepository>();
        _namespaces = Substitute.For<IServiceTemplateNamespaceProvider>();
    }

    private ActionRunner CreateSut(params IActionStrategy[] strategies) =>
        new(strategies, _repository, _namespaces, NullLogger<ActionRunner>.Instance);

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

        var result = await CreateSut(strategy).RunActionAsync(id, null, CancellationToken.None);

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

        var result = await CreateSut(strategy).RunActionAsync(action.Id, null, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Message.ShouldContain(nameof(IActionStrategy));
        await strategy.DidNotReceiveWithAnyArgs().ExecuteAsync(default!, default);
    }

    [Test]
    public async Task RunActionAsync_NoStrategiesRegistered_ReturnsNotFound()
    {
        var action = CustomActionTestData.Create(CustomActionTestData.Http());
        _repository.GetByIdAsync(action.Id, Arg.Any<CancellationToken>()).Returns(action);

        var result = await CreateSut().RunActionAsync(action.Id, null, CancellationToken.None);

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

        var result = await CreateSut(skipped, first, second).RunActionAsync(action.Id, null, CancellationToken.None);

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

        var result = await CreateSut(strategy).RunActionAsync(action.Id, null, CancellationToken.None);

        result.Error.ShouldBe(Error.Failed);
    }

    [Test]
    public async Task RunActionAsync_ResolvesTemplatesInConfig_WithoutMutatingAction()
    {
        var action = CustomActionTestData.Create(CustomActionTestData.Http(
            url: "https://${{ runtime.alias }}.local/${{ env.TOKEN | urlencode }}",
            headers: new Dictionary<string, string> { ["X-Key"] = "${{ env.TOKEN }}" },
            body: "${{ env.MISSING }}"));
        _repository.GetByIdAsync(action.Id, Arg.Any<CancellationToken>()).Returns(action);
        _namespaces.BuildAsync(action.ServiceId, Arg.Any<CancellationToken>()).Returns(
            new Dictionary<string, TemplateNamespaceResolver>
            {
                ["runtime"] = key => key == "alias" ? "web" : null,
                ["env"] = key => key == "TOKEN" ? "a b" : null
            });
        var strategy = Strategy(true);

        await CreateSut(strategy).RunActionAsync(action.Id, null, CancellationToken.None);

        var received = (HttpActionConfig)strategy.ReceivedCalls().Single(c => c.GetMethodInfo().Name == "ExecuteAsync")
            .GetArguments().OfType<CustomAction>().Single().Config;
        received.Url.ShouldBe("https://web.local/a%20b");
        received.Headers["X-Key"].ShouldBe("a b");
        received.Body.ShouldBe("${{ env.MISSING }}");
        ((HttpActionConfig)action.Config).Url.ShouldContain("${{");
    }

    private static CustomAction ActionWithInputs(params CustomActionInput[] inputs) =>
        CustomAction.Create(Guid.NewGuid(), "Run", "run", "desc", "play",
            CustomActionTestData.Http(url: "https://example.com/${{ inputs.target | urlencode }}"),
            [], ActionRisk.Safe, TimeSpan.FromSeconds(5), inputs);

    private static HttpActionConfig Received(IActionStrategy strategy) =>
        (HttpActionConfig)strategy.ReceivedCalls().Single(c => c.GetMethodInfo().Name == "ExecuteAsync")
            .GetArguments().OfType<CustomAction>().Single().Config;

    [Test]
    public async Task RunActionAsync_ResolvesSuppliedInputs()
    {
        var action = ActionWithInputs(new CustomActionInput("target", "Target", Required: true));
        _repository.GetByIdAsync(action.Id, Arg.Any<CancellationToken>()).Returns(action);
        var strategy = Strategy(true);

        var result = await CreateSut(strategy).RunActionAsync(action.Id,
            new Dictionary<string, string> { ["target"] = "a b" }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        Received(strategy).Url.ShouldBe("https://example.com/a%20b");
    }

    [Test]
    public async Task RunActionAsync_MissingInput_FallsBackToDefault()
    {
        var action = ActionWithInputs(new CustomActionInput("target", "Target", DefaultValue: "x"));
        _repository.GetByIdAsync(action.Id, Arg.Any<CancellationToken>()).Returns(action);
        var strategy = Strategy(true);

        await CreateSut(strategy).RunActionAsync(action.Id, null, CancellationToken.None);

        Received(strategy).Url.ShouldBe("https://example.com/x");
    }

    [Test]
    public async Task RunActionAsync_MissingRequiredInput_ReturnsValidationAndDoesNotExecute()
    {
        var action = ActionWithInputs(new CustomActionInput("target", "Target", Required: true));
        _repository.GetByIdAsync(action.Id, Arg.Any<CancellationToken>()).Returns(action);
        var strategy = Strategy(true);

        var result = await CreateSut(strategy).RunActionAsync(action.Id, null, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("VALIDATION");
        await strategy.DidNotReceiveWithAnyArgs().ExecuteAsync(default!, default);
    }

    [Test]
    public async Task RunActionAsync_UnknownInput_ReturnsValidation()
    {
        var action = ActionWithInputs(new CustomActionInput("target", "Target"));
        _repository.GetByIdAsync(action.Id, Arg.Any<CancellationToken>()).Returns(action);
        var strategy = Strategy(true);

        var result = await CreateSut(strategy).RunActionAsync(action.Id,
            new Dictionary<string, string> { ["bogus"] = "1" }, CancellationToken.None);

        result.Error.Code.ShouldBe("VALIDATION");
    }

    [Test]
    public async Task RunActionAsync_InputValueContainingPlaceholder_IsNotReResolved()
    {
        var action = ActionWithInputs(new CustomActionInput("target", "Target"));
        _repository.GetByIdAsync(action.Id, Arg.Any<CancellationToken>()).Returns(action);
        _namespaces.BuildAsync(action.ServiceId, Arg.Any<CancellationToken>()).Returns(
            new Dictionary<string, TemplateNamespaceResolver> { ["env"] = _ => "SECRET" });
        var strategy = Strategy(true);

        await CreateSut(strategy).RunActionAsync(action.Id,
            new Dictionary<string, string> { ["target"] = "${{ env.TOKEN }}" }, CancellationToken.None);

        Received(strategy).Url.ShouldNotContain("SECRET");
    }
}
