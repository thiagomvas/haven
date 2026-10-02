using FastEndpoints;

using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Infrastructure.Deployment;
using Haven.Infrastructure.Utils;

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
    private IServiceRepository _serviceRepository;

    [SetUp]
    public void SetUp()
    {
        _environmentVariableService = Substitute.For<IEnvironmentVariableService>();
        _featureFlagService = Substitute.For<IFeatureFlagService>();
        _secretVariableService = Substitute.For<ISecretVariableService>();
        _secretVariableService.GetSecretsAsEnvironmentVariablesForServiceAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _serviceRepository = Substitute.For<IServiceRepository>();
        _sut = new ContainerEnvironmentService(_environmentVariableService, _featureFlagService, _secretVariableService, _serviceRepository);
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
    [Test]
    public async Task BuildVariables_WithTemplatePlaceholders_ShouldResolveAllNamespaces()
    {
        var service = Service.Create(Guid.NewGuid(), "my-svc", ServiceType.DockerImage, ExposureMode.None, "api");
        _serviceRepository.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        _environmentVariableService.BuildVariablesForServiceAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns([
                new EnvironmentVariables { Key = "HOST", Value = "db" },
                new EnvironmentVariables { Key = "URL", Value = "http://${{ env.HOST }}/${{ runtime.name }}/${{ runtime.alias }}" },
                new EnvironmentVariables { Key = "CONN", Value = "pw=${{ secrets.PASS | urlencode }}" },
                new EnvironmentVariables { Key = "UNKNOWN", Value = "${{ inputs.x }}-${{ env.MISSING }}" }
            ]);
        _featureFlagService.GetFlagsAsEnvironmentsForServiceAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns([new EnvironmentVariables { Key = "FLAG_HOST", Value = "${{ env.HOST }}" }]);
        _secretVariableService.GetSecretsAsEnvironmentVariablesForServiceAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns([new EnvironmentVariables { Key = "PASS", Value = "a b" }]);

        var vars = (await _sut.BuildEnvironmentVariablesAsync(service.Id)).ToDictionary(v => v.Key, v => v.Value);

        vars["URL"].ShouldBe("http://db/my-svc/api");
        vars["CONN"].ShouldBe("pw=a%20b");
        vars["FLAG_HOST"].ShouldBe("db");
        vars["PASS"].ShouldBe("a b");
        vars["UNKNOWN"].ShouldBe("${{ inputs.x }}-${{ env.MISSING }}");
    }

    [Test]
    public async Task BuildVariables_WithHostnamePlaceholder_ShouldResolveToContainerName()
    {
        var service = Service.Create(Guid.NewGuid(), "my-svc", ServiceType.DockerImage, ExposureMode.None);
        _serviceRepository.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        _environmentVariableService.BuildVariablesForServiceAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns([new EnvironmentVariables { Key = "SELF", Value = "${{ runtime.hostname }}" }]);
        _featureFlagService.GetFlagsAsEnvironmentsForServiceAsync(service.Id, Arg.Any<CancellationToken>()).Returns([]);

        var vars = (await _sut.BuildEnvironmentVariablesAsync(service.Id)).ToList();

        vars[0].Value.ShouldBe(DockerUtils.BuildContainerName(null, null, null, service.Name, service.Id));
    }

    [Test]
    public async Task BuildVariables_ShouldNotMutateSourceEntities()
    {
        var service = Service.Create(Guid.NewGuid(), "svc", ServiceType.DockerImage, ExposureMode.None);
        var source = new EnvironmentVariables { Key = "A", Value = "${{ runtime.name }}" };
        _serviceRepository.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        _environmentVariableService.BuildVariablesForServiceAsync(service.Id, Arg.Any<CancellationToken>()).Returns([source]);
        _featureFlagService.GetFlagsAsEnvironmentsForServiceAsync(service.Id, Arg.Any<CancellationToken>()).Returns([]);

        var vars = (await _sut.BuildEnvironmentVariablesAsync(service.Id)).ToList();

        vars[0].Value.ShouldBe("svc");
        source.Value.ShouldBe("${{ runtime.name }}");
    }
}