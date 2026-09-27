using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Features.Services.ComputedOutputs;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;

using NSubstitute;

using Shouldly;

using DomainEnvironmentVariables = Haven.Domain.Entities.EnvironmentVariables;

namespace Haven.Application.Tests.Features.Services.ComputedOutputs;

[TestFixture]
[Category("Unit")]
public class ComputedOutputResolverTests
{
    private IEnvironmentVariableService _environmentVariableService = null!;
    private ISecretVariableService _secretVariableService = null!;
    private ComputedOutputResolver _sut = null!;

    [SetUp]
    public void Setup()
    {
        _environmentVariableService = Substitute.For<IEnvironmentVariableService>();
        _secretVariableService = Substitute.For<ISecretVariableService>();
        _sut = new ComputedOutputResolver(_environmentVariableService, _secretVariableService);
    }

    private static Service NewServiceWithOutput(string template, bool isSecret = true)
    {
        var service = Service.Create(Guid.NewGuid(), "svc", ServiceType.DockerImage, ExposureMode.None);
        service.AddComputedProperty("connection_string", "Connection String", template, isSecret);
        return service;
    }

    private static ServiceRegistryEntry Registry(string ip, ServiceStatus status = ServiceStatus.Running)
    {
        var entry = ServiceRegistryEntry.Create(Guid.NewGuid());
        entry.UpdateRuntime(ip, [], status);
        return entry;
    }

    [Test]
    public async Task ResolveAsync_NoComputedProperties_ReturnsEmpty()
    {
        var service = Service.Create(Guid.NewGuid(), "svc", ServiceType.DockerImage, ExposureMode.None);

        var result = await _sut.ResolveAsync(service, null, CancellationToken.None);

        result.ShouldBeEmpty();
    }

    [Test]
    public async Task ResolveAsync_RunningWithIp_ProducesFullValueAndMaskedPreview()
    {
        var service = NewServiceWithOutput("postgresql://${{ env.POSTGRES_USER }}:${{ env.POSTGRES_PASSWORD }}@${{ runtime.host }}:5432/db");
        _environmentVariableService.BuildVariablesForServiceAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns([new DomainEnvironmentVariables { Key = "POSTGRES_USER", Value = "postgres" }]);
        _secretVariableService.GetSecretsAsEnvironmentVariablesForServiceAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns([new DomainEnvironmentVariables { Key = "POSTGRES_PASSWORD", Value = "s3cr3t" }]);

        var result = await _sut.ResolveAsync(service, Registry("172.18.0.5"), CancellationToken.None);

        var output = result.Single();
        output.IsAvailable.ShouldBeTrue();
        output.Value.ShouldBe("postgresql://postgres:s3cr3t@172.18.0.5:5432/db");
        output.MaskedPreview.ShouldNotContain("s3cr3t");
        output.MaskedPreview.ShouldContain("postgres:");
    }

    [Test]
    public async Task ResolveAsync_NoRegistry_ReportsNotRunning()
    {
        var service = NewServiceWithOutput("redis://${{ runtime.host }}:6379");
        _environmentVariableService.BuildVariablesForServiceAsync(service.Id, Arg.Any<CancellationToken>()).Returns([]);
        _secretVariableService.GetSecretsAsEnvironmentVariablesForServiceAsync(service.Id, Arg.Any<CancellationToken>()).Returns([]);

        var result = await _sut.ResolveAsync(service, null, CancellationToken.None);

        var output = result.Single();
        output.IsAvailable.ShouldBeFalse();
        output.UnavailableReason.ShouldBe("NotRunning");
    }

    [Test]
    public async Task ResolveAsync_MissingEnvVar_ReportsMissingVariable()
    {
        var service = NewServiceWithOutput("redis://:${{ env.REDIS_PASSWORD }}@${{ runtime.host }}:6379");
        _environmentVariableService.BuildVariablesForServiceAsync(service.Id, Arg.Any<CancellationToken>()).Returns([]);
        _secretVariableService.GetSecretsAsEnvironmentVariablesForServiceAsync(service.Id, Arg.Any<CancellationToken>()).Returns([]);

        var result = await _sut.ResolveAsync(service, Registry("172.18.0.5"), CancellationToken.None);

        var output = result.Single();
        output.IsAvailable.ShouldBeFalse();
        output.UnavailableReason.ShouldBe("MissingVariable:REDIS_PASSWORD");
    }

    [Test]
    public async Task ResolveAsync_SecretOverridesPlainVarWithSameKey()
    {
        var service = NewServiceWithOutput("${{ env.KEY }}");
        _environmentVariableService.BuildVariablesForServiceAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns([new DomainEnvironmentVariables { Key = "KEY", Value = "plain" }]);
        _secretVariableService.GetSecretsAsEnvironmentVariablesForServiceAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns([new DomainEnvironmentVariables { Key = "KEY", Value = "secretvalue" }]);

        var result = await _sut.ResolveAsync(service, Registry("172.18.0.5"), CancellationToken.None);

        result.Single().Value.ShouldBe("secretvalue");
    }

    [Test]
    public async Task ResolveAsync_PasswordWithSpecialChars_GetsUrlencoded()
    {
        var service = NewServiceWithOutput("postgresql://user:${{ env.PASSWORD | urlencode }}@${{ runtime.host }}:5432/db");
        _environmentVariableService.BuildVariablesForServiceAsync(service.Id, Arg.Any<CancellationToken>()).Returns([]);
        _secretVariableService.GetSecretsAsEnvironmentVariablesForServiceAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns([new DomainEnvironmentVariables { Key = "PASSWORD", Value = "p@ss/word" }]);

        var result = await _sut.ResolveAsync(service, Registry("172.18.0.5"), CancellationToken.None);

        result.Single().Value.ShouldBe($"postgresql://user:{Uri.EscapeDataString("p@ss/word")}@172.18.0.5:5432/db");
    }

    [Test]
    public async Task ResolveAsync_RabbitmqDefaultVhost_EncodesSlashAsPercent2F()
    {
        var service = NewServiceWithOutput("amqp://guest:${{ env.PASS | urlencode }}@${{ runtime.host }}:5672/${{ env.VHOST | urlencode }}", isSecret: true);
        _environmentVariableService.BuildVariablesForServiceAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns([new DomainEnvironmentVariables { Key = "VHOST", Value = "/" }]);
        _secretVariableService.GetSecretsAsEnvironmentVariablesForServiceAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns([new DomainEnvironmentVariables { Key = "PASS", Value = "guest" }]);

        var result = await _sut.ResolveAsync(service, Registry("172.18.0.5"), CancellationToken.None);

        result.Single().Value.ShouldEndWith("/%2F");
    }
}