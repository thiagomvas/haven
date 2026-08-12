using Haven.Application.Common.Interfaces;
using Haven.Application.Features.EnvironmentVariables.Queries.ExportEnvExample;
using Haven.Domain.Enums;

using NSubstitute;

using Shouldly;

using DomainEnvironmentVariables = Haven.Domain.Entities.EnvironmentVariables;

namespace Haven.Application.Tests.Features.EnvironmentVariables.Queries.ExportEnvExample;

[Category("Unit")]
public sealed class ExportEnvExampleHandlerTests
{
    private IEnvironmentVariableService _environmentVariableService = null!;
    private IEnvironmentVariableSerializer _serializer = null!;
    private IFeatureFlagService _featureFlagService = null!;
    private ExportEnvExampleHandler _sut = null!;

    [SetUp]
    public void Setup()
    {
        _environmentVariableService = Substitute.For<IEnvironmentVariableService>();
        _serializer = Substitute.For<IEnvironmentVariableSerializer>();
        _featureFlagService = Substitute.For<IFeatureFlagService>();
        _sut = new ExportEnvExampleHandler(_environmentVariableService, _serializer, _featureFlagService);

        _serializer.Serialize(Arg.Any<IEnumerable<DomainEnvironmentVariables>>(), Arg.Any<bool>())
            .Returns(callInfo => string.Join('\n',
                callInfo.Arg<IEnumerable<DomainEnvironmentVariables>>().Select(v => $"{v.Key}={v.Value}")));
    }

    [Test]
    public async Task Handle_ForProject_ShouldBuildFromProjectVariables()
    {
        var query = new ExportEnvExampleQuery
        {
            ParentId = Guid.NewGuid(),
            ParentType = EnvironmentVariableParentType.Project,
            IncludeValues = true
        };
        _environmentVariableService.BuildVariablesForProjectAsync(query.ParentId, Arg.Any<CancellationToken>())
            .Returns([Var("KEY", "value")]);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("KEY=value");
        await _featureFlagService.DidNotReceive()
            .GetFlagsAsEnvironmentsForServiceAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ForEnvironment_ShouldBuildFromEnvironmentVariables()
    {
        var query = new ExportEnvExampleQuery
        {
            ParentId = Guid.NewGuid(),
            ParentType = EnvironmentVariableParentType.Environment,
            IncludeValues = true
        };
        _environmentVariableService.BuildVariablesForEnvironmentAsync(query.ParentId, Arg.Any<CancellationToken>())
            .Returns([Var("KEY", "value")]);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("KEY=value");
        await _featureFlagService.DidNotReceive()
            .GetFlagsAsEnvironmentsForServiceAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ForService_WhenIncludeFeatureFlagsTrue_ShouldAppendFlags()
    {
        var query = new ExportEnvExampleQuery
        {
            ParentId = Guid.NewGuid(),
            ParentType = EnvironmentVariableParentType.Service,
            IncludeValues = true,
            IncludeFeatureFlags = true
        };
        _environmentVariableService.BuildVariablesForServiceAsync(query.ParentId, Arg.Any<CancellationToken>())
            .Returns([Var("KEY", "value")]);
        _featureFlagService.GetFlagsAsEnvironmentsForServiceAsync(query.ParentId, Arg.Any<CancellationToken>())
            .Returns([Var("FLAG_ENABLED", "true")]);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldContain("FLAG_ENABLED=true");
        result.Value.ShouldContain("KEY=value");
    }

    [Test]
    public async Task Handle_ForService_WhenIncludeFeatureFlagsFalse_ShouldNotAppendFlags()
    {
        var query = new ExportEnvExampleQuery
        {
            ParentId = Guid.NewGuid(),
            ParentType = EnvironmentVariableParentType.Service,
            IncludeValues = true,
            IncludeFeatureFlags = false
        };
        _environmentVariableService.BuildVariablesForServiceAsync(query.ParentId, Arg.Any<CancellationToken>())
            .Returns([Var("KEY", "value")]);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("KEY=value");
        await _featureFlagService.DidNotReceive()
            .GetFlagsAsEnvironmentsForServiceAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ForProject_WhenIncludeFeatureFlagsTrue_ShouldBeIgnored()
    {
        var query = new ExportEnvExampleQuery
        {
            ParentId = Guid.NewGuid(),
            ParentType = EnvironmentVariableParentType.Project,
            IncludeValues = true,
            IncludeFeatureFlags = true
        };
        _environmentVariableService.BuildVariablesForProjectAsync(query.ParentId, Arg.Any<CancellationToken>())
            .Returns([Var("KEY", "value")]);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _featureFlagService.DidNotReceive()
            .GetFlagsAsEnvironmentsForServiceAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ShouldPassIncludeValues_ToSerializer()
    {
        var query = new ExportEnvExampleQuery
        {
            ParentId = Guid.NewGuid(),
            ParentType = EnvironmentVariableParentType.Project,
            IncludeValues = false
        };
        _environmentVariableService.BuildVariablesForProjectAsync(query.ParentId, Arg.Any<CancellationToken>())
            .Returns([Var("KEY", "value")]);

        await _sut.Handle(query, CancellationToken.None);

        _serializer.Received(1).Serialize(Arg.Any<IEnumerable<DomainEnvironmentVariables>>(), false);
    }

    [Test]
    public async Task Handle_ShouldOrderVariablesByKey_BeforeSerializing()
    {
        var query = new ExportEnvExampleQuery
        {
            ParentId = Guid.NewGuid(),
            ParentType = EnvironmentVariableParentType.Project,
            IncludeValues = true
        };
        _environmentVariableService.BuildVariablesForProjectAsync(query.ParentId, Arg.Any<CancellationToken>())
            .Returns([Var("ZEBRA", "z"), Var("ALPHA", "a")]);

        await _sut.Handle(query, CancellationToken.None);

        string[] expectedOrder = ["ALPHA", "ZEBRA"];
        _serializer.Received(1).Serialize(
            Arg.Is<IEnumerable<DomainEnvironmentVariables>>(v => v.Select(x => x.Key).SequenceEqual(expectedOrder)),
            Arg.Any<bool>());
    }

    private static DomainEnvironmentVariables Var(string key, string? value) =>
        new() { Key = key, Value = value };
}
