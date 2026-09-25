using FastEndpoints;

using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Domain.Entities;
using Haven.Infrastructure.Deployment;

using NSubstitute;

using Org.BouncyCastle.Security;

using Shouldly;

namespace Haven.Infrastructure.Tests.Deployment;

[TestFixture]
[Category("Unit")]
public class ContainerEnvironmentServiceTests
{
    private ContainerEnvironmentService _sut;
    private IEnvironmentVariableService _environmentVariableService;
    private IFeatureFlagService _featureFlagService;
    private ISecretVariableService _secretVariableService;

    [SetUp]
    public void SetUp()
    {
        _environmentVariableService = Substitute.For<IEnvironmentVariableService>();
        _featureFlagService = Substitute.For<IFeatureFlagService>();
        _secretVariableService = Substitute.For<ISecretVariableService>();
        _secretVariableService.GetSecretsAsEnvironmentVariablesForServiceAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _sut = new ContainerEnvironmentService(_environmentVariableService, _featureFlagService, _secretVariableService);
    }

    [Test]
    public async Task BuildVariables_WithOnlyEnvs_ShouldBuildSuccessfully()
    {
        _environmentVariableService.BuildVariablesForServiceAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([new EnvironmentVariables() { Key = "ENV1", Value = "Value1" }]);

        _featureFlagService.GetFlagsAsEnvironmentsForServiceAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await _sut.BuildEnvironmentVariablesAsync(Guid.NewGuid());
        var vars = result.ToList();

        vars.ShouldNotBeNull();
        vars.ShouldNotBeEmpty();
        vars.Count.ShouldBe(1);
        vars[0].Key.ShouldBe("ENV1");
        vars[0].Value.ShouldBe("Value1");
    }

    [Test]
    public async Task BuildVariables_WithOnlyFlags_ShouldBuildSuccessfully()
    {
        _environmentVariableService.BuildVariablesForServiceAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([]);

        _featureFlagService.GetFlagsAsEnvironmentsForServiceAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([new EnvironmentVariables() { Key = "FLAG1", Value = "Value1" }]);

        var result = await _sut.BuildEnvironmentVariablesAsync(Guid.NewGuid());
        var vars = result.ToList();

        vars.ShouldNotBeNull();
        vars.ShouldNotBeEmpty();
        vars.Count.ShouldBe(1);
        vars[0].Key.ShouldBe("FLAG1");
        vars[0].Value.ShouldBe("Value1");
    }

    [Test]
    public async Task BuildVariables_WithBoth_ShouldBuildSuccessfully()
    {
        _environmentVariableService.BuildVariablesForServiceAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([new EnvironmentVariables() { Key = "ENV1", Value = "Value1" }]);

        _featureFlagService.GetFlagsAsEnvironmentsForServiceAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([new EnvironmentVariables() { Key = "FLAG1", Value = "Value1" }]);

        var result = await _sut.BuildEnvironmentVariablesAsync(Guid.NewGuid());
        var vars = result.ToList();

        vars.ShouldNotBeNull();
        vars.ShouldNotBeEmpty();
        vars.Count.ShouldBe(2);
        vars.ShouldContain(v => v.Key == "ENV1" && v.Value == "Value1");
        vars.ShouldContain(v => v.Key == "FLAG1" && v.Value == "Value1");
    }

    [Test]
    public async Task BuildVariables_WithOverrides_ShouldBuildSuccessfully()
    {
        _environmentVariableService.BuildVariablesForServiceAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([new EnvironmentVariables() { Key = "ENV1", Value = "Value1" }]);

        _featureFlagService.GetFlagsAsEnvironmentsForServiceAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([new EnvironmentVariables() { Key = "ENV1", Value = "OverriddenValue" }]);

        var result = await _sut.BuildEnvironmentVariablesAsync(Guid.NewGuid());
        var vars = result.ToList();

        vars.ShouldNotBeNull();
        vars.ShouldNotBeEmpty();
        vars.Count.ShouldBe(1);
        vars[0].Key.ShouldBe("ENV1");
        vars[0].Value.ShouldBe("OverriddenValue");
    }

    [Test]
    public async Task BuildVariables_WithEmpty_ShouldBuildSuccessfully()
    {
        _environmentVariableService.BuildVariablesForServiceAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([]);

        _featureFlagService.GetFlagsAsEnvironmentsForServiceAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await _sut.BuildEnvironmentVariablesAsync(Guid.NewGuid());
        var vars = result.ToList();

        vars.ShouldNotBeNull();
        vars.ShouldBeEmpty();
    }
}