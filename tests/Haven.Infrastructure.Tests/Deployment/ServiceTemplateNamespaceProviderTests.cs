using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Templating;
using Haven.Application.Features.Services.ComputedOutputs;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Infrastructure.Deployment;

using NSubstitute;

using Shouldly;

namespace Haven.Infrastructure.Tests.Deployment;

[TestFixture]
[Category("Unit")]
public sealed class ServiceTemplateNamespaceProviderTests
{
    private const string MaskedPlaceholder = "••••••••";

    private IEnvironmentVariableService _environmentVariableService = null!;
    private IFeatureFlagService _featureFlagService = null!;
    private ISecretVariableService _secretVariableService = null!;
    private IServiceRepository _serviceRepository = null!;
    private IServiceRegistryEntryRepository _registryRepository = null!;
    private Service _service = null!;
    private List<Service> _environmentServices = null!;

    [SetUp]
    public void Setup()
    {
        _environmentVariableService = Substitute.For<IEnvironmentVariableService>();
        _featureFlagService = Substitute.For<IFeatureFlagService>();
        _secretVariableService = Substitute.For<ISecretVariableService>();
        _serviceRepository = Substitute.For<IServiceRepository>();
        _registryRepository = Substitute.For<IServiceRegistryEntryRepository>();

        _service = Service.Create(Guid.NewGuid(), "test-svc", ServiceType.DockerImage, ExposureMode.Internal);

        _serviceRepository.GetByIdAsync(_service.Id, Arg.Any<CancellationToken>()).Returns(_service);
        _environmentServices = [_service];
        _serviceRepository.GetByEnvironmentIdAsync(_service.EnvironmentId, Arg.Any<CancellationToken>())
            .Returns(_environmentServices.ToList());
        _environmentVariableService.BuildVariablesForServiceAsync(_service.Id, Arg.Any<CancellationToken>())
            .Returns([]);
        _featureFlagService.GetFlagsAsEnvironmentsForServiceAsync(_service.Id, Arg.Any<CancellationToken>())
            .Returns([]);
        _secretVariableService
            .GetSecretsAsEnvironmentVariablesForServiceAsync(_service.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<EnvironmentVariables>());
        _registryRepository.GetForServiceAsync(_service.Id, Arg.Any<CancellationToken>())
            .Returns((ServiceRegistryEntry?)null);
    }

    // Uses the real resolver so the tests prove the end-to-end value flow, including masking behaviour.
    private ServiceTemplateNamespaceProvider CreateSut() =>
        new(_environmentVariableService, _featureFlagService, _secretVariableService, _serviceRepository,
            _registryRepository, new ComputedOutputResolver(_environmentVariableService, _secretVariableService));

    private void GivenEnv(string key, string value) =>
        _environmentVariableService.BuildVariablesForServiceAsync(_service.Id, Arg.Any<CancellationToken>())
            .Returns([new EnvironmentVariables { Key = key, Value = value }]);

    private void GivenSecret(string key, string value) =>
        _secretVariableService
            .GetSecretsAsEnvironmentVariablesForServiceAsync(_service.Id, Arg.Any<CancellationToken>())
            .Returns([new EnvironmentVariables { Key = key, Value = value }]);

    private void GivenRunningAt(string ip)
    {
        var registry = ServiceRegistryEntry.Create(_service.Id);
        registry.IpAddress = ip;
        _registryRepository.GetForServiceAsync(_service.Id, Arg.Any<CancellationToken>()).Returns(registry);
    }

    private async Task<string?> ResolveOutputAsync(string key)
    {
        var namespaces = await CreateSut().BuildAsync(_service.Id);
        namespaces.ShouldNotBeNull();
        return namespaces[TemplateNamespaces.Outputs](key);
    }

    [Test]
    public async Task BuildAsync_WhenServiceNotFound_ShouldReturnNull()
    {
        _serviceRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Service?)null);

        var result = await CreateSut().BuildAsync(Guid.NewGuid());

        result.ShouldBeNull();
    }

    [Test]
    public async Task BuildAsync_ShouldAlwaysExposeOutputsNamespace()
    {
        var result = await CreateSut().BuildAsync(_service.Id);

        result.ShouldNotBeNull();
        result.ShouldContainKey(TemplateNamespaces.Outputs);
    }

    [Test]
    public async Task BuildAsync_WhenServiceHasNoComputedProperties_ShouldResolveOutputsToNull()
    {
        (await ResolveOutputAsync("anything")).ShouldBeNull();
    }

    [Test]
    public async Task BuildAsync_ShouldExposeComputedPropertyByKey()
    {
        _service.AddComputedProperty("url", "URL", "http://${{ env.HOST_NAME }}:5432", isSecret: false);
        GivenEnv("HOST_NAME", "db.local");

        (await ResolveOutputAsync("url")).ShouldBe("http://db.local:5432");
    }

    [Test]
    public async Task BuildAsync_ShouldExposeEveryComputedProperty()
    {
        _service.AddComputedProperty("a", "A", "value-a", isSecret: false);
        _service.AddComputedProperty("b", "B", "value-b", isSecret: false);

        var namespaces = await CreateSut().BuildAsync(_service.Id);

        namespaces.ShouldNotBeNull();
        namespaces[TemplateNamespaces.Outputs]("a").ShouldBe("value-a");
        namespaces[TemplateNamespaces.Outputs]("b").ShouldBe("value-b");
    }

    [Test]
    public async Task BuildAsync_WhenComputedPropertyIsSecret_ShouldExposeUnmaskedValue()
    {
        _service.AddComputedProperty("conn", "Connection", "postgres://app:${{ env.DB_PASSWORD }}@db", isSecret: true);
        GivenSecret("DB_PASSWORD", "s3cr3t");

        var value = await ResolveOutputAsync("conn");

        value.ShouldNotBeNull();
        value.ShouldBe("postgres://app:s3cr3t@db");
        value.ShouldNotContain(MaskedPlaceholder);
    }

    [Test]
    public async Task BuildAsync_WhenSecretValueNeedsEscaping_ShouldExposeValueExactlyAsResolved()
    {
        _service.AddComputedProperty("conn", "Connection", "postgres://app:${{ env.DB_PASSWORD }}@db", isSecret: true);
        GivenSecret("DB_PASSWORD", "p@ss/word");

        var value = await ResolveOutputAsync("conn");

        value.ShouldNotBeNull();
        value.ShouldContain("p@ss/word");
        value.ShouldNotContain(MaskedPlaceholder);
    }

    [Test]
    public async Task BuildAsync_WhenComputedPropertyReferencesRuntimeHost_ShouldResolveFromRegistry()
    {
        _service.AddComputedProperty("addr", "Address", "${{ runtime.host }}:6379", isSecret: false);
        GivenRunningAt("172.20.0.5");

        (await ResolveOutputAsync("addr")).ShouldBe("172.20.0.5:6379");
    }

    [Test]
    public async Task BuildAsync_WhenServiceIsNotRunning_ShouldNotExposeHostDependentOutput()
    {
        _service.AddComputedProperty("addr", "Address", "${{ runtime.host }}:6379", isSecret: false);

        (await ResolveOutputAsync("addr")).ShouldBeNull();
    }

    [Test]
    public async Task BuildAsync_WhenReferencedVariableIsMissing_ShouldNotExposeOutput()
    {
        _service.AddComputedProperty("url", "URL", "http://${{ env.MISSING }}", isSecret: false);

        (await ResolveOutputAsync("url")).ShouldBeNull();
    }

    [Test]
    public async Task BuildAsync_WhenOneOutputIsUnavailable_ShouldStillExposeTheOthers()
    {
        _service.AddComputedProperty("broken", "Broken", "${{ env.MISSING }}", isSecret: false);
        _service.AddComputedProperty("ok", "Ok", "fine", isSecret: false);

        var namespaces = await CreateSut().BuildAsync(_service.Id);

        namespaces.ShouldNotBeNull();
        namespaces[TemplateNamespaces.Outputs]("broken").ShouldBeNull();
        namespaces[TemplateNamespaces.Outputs]("ok").ShouldBe("fine");
    }

    [Test]
    public async Task BuildAsync_WhenKeyIsUnknown_ShouldResolveToNull()
    {
        _service.AddComputedProperty("known", "Known", "value", isSecret: false);

        (await ResolveOutputAsync("unknown")).ShouldBeNull();
    }

    [Test]
    public async Task BuildAsync_ShouldPassServiceRegistryEntryToResolver()
    {
        var resolver = Substitute.For<IComputedOutputResolver>();
        resolver.ResolveAsync(Arg.Any<Service>(), Arg.Any<ServiceRegistryEntry?>(), Arg.Any<CancellationToken>())
            .Returns([new ResolvedComputedOutput("k", "K", false, true, null, "v", "v")]);
        var registry = ServiceRegistryEntry.Create(_service.Id);
        _registryRepository.GetForServiceAsync(_service.Id, Arg.Any<CancellationToken>()).Returns(registry);

        var sut = new ServiceTemplateNamespaceProvider(_environmentVariableService, _featureFlagService,
            _secretVariableService, _serviceRepository, _registryRepository, resolver);

        var namespaces = await sut.BuildAsync(_service.Id);

        await resolver.Received(1).ResolveAsync(_service, registry, Arg.Any<CancellationToken>());
        namespaces.ShouldNotBeNull();
        namespaces[TemplateNamespaces.Outputs]("k").ShouldBe("v");
    }

    [Test]
    public async Task BuildAsync_ShouldUseRawValueNotMaskedPreview()
    {
        var resolver = Substitute.For<IComputedOutputResolver>();
        resolver.ResolveAsync(Arg.Any<Service>(), Arg.Any<ServiceRegistryEntry?>(), Arg.Any<CancellationToken>())
            .Returns([new ResolvedComputedOutput("k", "K", true, true, null, "raw-value", MaskedPlaceholder)]);

        var sut = new ServiceTemplateNamespaceProvider(_environmentVariableService, _featureFlagService,
            _secretVariableService, _serviceRepository, _registryRepository, resolver);

        var namespaces = await sut.BuildAsync(_service.Id);

        namespaces.ShouldNotBeNull();
        namespaces[TemplateNamespaces.Outputs]("k").ShouldBe("raw-value");
    }

    [Test]
    public async Task BuildAsync_ShouldNotBreakExistingNamespaces()
    {
        GivenEnv("FOO", "bar");
        GivenSecret("TOKEN", "abc");
        _service.AddComputedProperty("x", "X", "y", isSecret: false);

        var namespaces = await CreateSut().BuildAsync(_service.Id);

        namespaces.ShouldNotBeNull();
        namespaces[TemplateNamespaces.EnvVariables]("FOO").ShouldBe("bar");
        namespaces[TemplateNamespaces.Secrets]("TOKEN").ShouldBe("abc");
        namespaces[TemplateNamespaces.Runtime]("name").ShouldBe("test-svc");
    }

    private Service GivenSibling(string name, string? alias, params (string Key, string Value)[] envs)
    {
        var sibling = Service.Create(_service.EnvironmentId, name, ServiceType.DockerImage, ExposureMode.Internal, alias);
        _serviceRepository.GetByIdAsync(sibling.Id, Arg.Any<CancellationToken>()).Returns(sibling);
        _environmentVariableService.BuildVariablesForServiceAsync(sibling.Id, Arg.Any<CancellationToken>())
            .Returns(envs.Select(e => new EnvironmentVariables { Key = e.Key, Value = e.Value }).ToList());
        _featureFlagService.GetFlagsAsEnvironmentsForServiceAsync(sibling.Id, Arg.Any<CancellationToken>())
            .Returns([]);
        _secretVariableService
            .GetSecretsAsEnvironmentVariablesForServiceAsync(sibling.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<EnvironmentVariables>());
        _registryRepository.GetForServiceAsync(sibling.Id, Arg.Any<CancellationToken>())
            .Returns((ServiceRegistryEntry?)null);

        _environmentServices.Add(sibling);
        _serviceRepository.GetByEnvironmentIdAsync(_service.EnvironmentId, Arg.Any<CancellationToken>())
            .Returns(_environmentServices.ToList());
        return sibling;
    }

    private async Task<string?> ResolveServiceAsync(string key)
    {
        var namespaces = await CreateSut().BuildAsync(_service.Id);
        namespaces.ShouldNotBeNull();
        return namespaces[TemplateNamespaces.Services](key);
    }

    [Test]
    public async Task BuildAsync_ShouldAlwaysExposeServicesNamespace()
    {
        var result = await CreateSut().BuildAsync(_service.Id);

        result.ShouldNotBeNull();
        result.ShouldContainKey(TemplateNamespaces.Services);
    }

    [Test]
    public async Task BuildAsync_ShouldResolveSiblingComputedOutputByAlias()
    {
        var db = GivenSibling("db-svc", "db", ("HOST", "pg.local"));
        db.AddComputedProperty("connection_string", "Connection", "postgres://${{ env.HOST }}:5432", isSecret: false);

        (await ResolveServiceAsync("db.outputs.connection_string")).ShouldBe("postgres://pg.local:5432");
    }

    [Test]
    public async Task BuildAsync_ShouldResolveSiblingEnvAndRuntime()
    {
        GivenSibling("db-svc", "db", ("PORT", "5432"));

        (await ResolveServiceAsync("db.env.PORT")).ShouldBe("5432");
        (await ResolveServiceAsync("db.runtime.name")).ShouldBe("db-svc");
    }

    [Test]
    public async Task BuildAsync_ShouldKeepDotsInInnerKey()
    {
        GivenSibling("db-svc", "db", ("A.B", "dotted"));

        (await ResolveServiceAsync("db.env.A.B")).ShouldBe("dotted");
    }

    [Test]
    public async Task BuildAsync_ShouldResolveMultipleSiblingsIndependently()
    {
        GivenSibling("db-svc", "db", ("X", "from-db"));
        GivenSibling("cache-svc", "cache", ("X", "from-cache"));

        (await ResolveServiceAsync("db.env.X")).ShouldBe("from-db");
        (await ResolveServiceAsync("cache.env.X")).ShouldBe("from-cache");
    }

    [Test]
    public async Task BuildAsync_WhenAliasUnknown_ShouldResolveToNull()
    {
        GivenSibling("db-svc", "db", ("X", "1"));

        (await ResolveServiceAsync("nope.env.X")).ShouldBeNull();
    }

    [Test]
    public async Task BuildAsync_WhenScopeUnknown_ShouldResolveToNull()
    {
        GivenSibling("db-svc", "db", ("X", "1"));

        (await ResolveServiceAsync("db.nope.X")).ShouldBeNull();
    }

    [Test]
    public async Task BuildAsync_WhenSiblingKeyMissing_ShouldResolveToNull()
    {
        GivenSibling("db-svc", "db", ("X", "1"));

        (await ResolveServiceAsync("db.env.MISSING")).ShouldBeNull();
    }

    [Test]
    public async Task BuildAsync_ShouldNotExposeServiceItself()
    {
        _service.Alias = "me";
        GivenSibling("db-svc", "db");

        (await ResolveServiceAsync("me.runtime.name")).ShouldBeNull();
    }

    [Test]
    public async Task BuildAsync_ShouldSkipSiblingsWithoutAlias()
    {
        GivenSibling("anon-svc", null, ("X", "1"));

        (await ResolveServiceAsync(".env.X")).ShouldBeNull();
        (await ResolveServiceAsync("anon-svc.env.X")).ShouldBeNull();
    }

    [Test]
    public async Task BuildAsync_ShouldNotNestServicesNamespaceInSiblings()
    {
        GivenSibling("db-svc", "db", ("X", "1"));
        GivenSibling("cache-svc", "cache", ("X", "2"));

        (await ResolveServiceAsync("db.services.cache.env.X")).ShouldBeNull();
    }

    [Test]
    public async Task BuildAsync_WhenAliasedServiceReferencesSibling_ShouldNotRecurse()
    {
        // Siblings are built without a services namespace, so a cross-reference stays an
        // unresolved placeholder instead of recursing.
        var db = GivenSibling("db-svc", "db");
        db.AddComputedProperty("loop", "Loop", "${{ services.app.outputs.loop }}", isSecret: false);

        (await ResolveServiceAsync("db.outputs.loop")).ShouldBe("${{ services.app.outputs.loop }}");
    }

    [Test]
    public async Task BuildAsync_ShouldQuerySiblingsOfTheServicesEnvironment()
    {
        GivenSibling("db-svc", "db");

        await CreateSut().BuildAsync(_service.Id);

        await _serviceRepository.Received().GetByEnvironmentIdAsync(_service.EnvironmentId, Arg.Any<CancellationToken>());
    }
}
