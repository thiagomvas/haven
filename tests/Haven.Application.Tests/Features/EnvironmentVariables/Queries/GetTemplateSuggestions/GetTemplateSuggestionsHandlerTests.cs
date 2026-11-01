using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Templating;
using Haven.Application.Features.EnvironmentVariables.Queries.GetTemplateSuggestions;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;

using NSubstitute;

using Shouldly;

using Environment = Haven.Domain.Aggregates.Environment;
using EnvVar = Haven.Domain.Entities.EnvironmentVariables;

namespace Haven.Application.Tests.Features.EnvironmentVariables.Queries.GetTemplateSuggestions;

[TestFixture]
[Category("Unit")]
public class GetTemplateSuggestionsHandlerTests
{
    private IProjectRepository _projects = null!;
    private IEnvironmentRepository _environments = null!;
    private IServiceRepository _services = null!;
    private ISecretVariableRepository _secrets = null!;
    private IEnvironmentVariableService _envService = null!;
    private IFeatureFlagService _flags = null!;
    private GetTemplateSuggestionsHandler _sut = null!;

    [SetUp]
    public void Setup()
    {
        _projects = Substitute.For<IProjectRepository>();
        _environments = Substitute.For<IEnvironmentRepository>();
        _services = Substitute.For<IServiceRepository>();
        _secrets = Substitute.For<ISecretVariableRepository>();
        _envService = Substitute.For<IEnvironmentVariableService>();
        _flags = Substitute.For<IFeatureFlagService>();

        _secrets.GetForParentAsync(default, default, default)
            .ReturnsForAnyArgs(Task.FromResult<IEnumerable<SecretVariable>>([]));
        _envService.BuildVariablesForServiceAsync(default, default).ReturnsForAnyArgs([]);
        _flags.GetFlagsAsEnvironmentsForServiceAsync(default, default)
            .ReturnsForAnyArgs(Task.FromResult<IReadOnlyList<EnvVar>>([]));

        _sut = new GetTemplateSuggestionsHandler(_projects, _environments, _services, _secrets, _envService, _flags);
    }

    private static Service NewService(Guid environmentId, string alias) =>
        Service.Create(environmentId, alias, ServiceType.DockerImage, ExposureMode.None, alias);

    [Test]
    public async Task Handle_ServiceScope_ReturnsEveryNamespaceAndSiblingKeys()
    {
        var environment = Environment.Create(Guid.NewGuid(), "dev");
        var service = NewService(environment.Id, "api");
        var db = NewService(environment.Id, "db");
        db.AddComputedProperty("connection_string", "Connection String", "template", true);

        _environments.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>()).Returns(environment);
        _services.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        _services.GetByEnvironmentIdAsync(environment.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Service> { service, db });
        _envService.BuildVariablesForServiceAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns([new EnvVar { Key = "PORT" }]);
        _secrets.GetForParentAsync(service.Id, EnvironmentVariableParentType.Service, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<SecretVariable>>([new SecretVariable { Key = "API_KEY" }]));

        var result = await _sut.Handle(
            new GetTemplateSuggestionsQuery { ParentId = service.Id, ParentType = EnvironmentVariableParentType.Service },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.IsExhaustive.ShouldBeTrue();
        result.Value.Namespaces.ShouldBe(["env", "secrets", "runtime", "outputs", "services"]);
        result.Value.Filters.ShouldContain("urlencode");

        var pairs = result.Value.Variables.Select(v => $"{v.Namespace}.{v.Key}").ToList();
        pairs.ShouldContain("env.PORT");
        pairs.ShouldContain("secrets.API_KEY");
        pairs.ShouldContain("runtime.hostname");
        pairs.ShouldContain("services.db.outputs.connection_string");
        pairs.ShouldContain("services.db.runtime.hostname");
        pairs.ShouldNotContain(p => p.StartsWith("services.api."));
    }

    [Test]
    public async Task Handle_EnvironmentScope_OmitsServiceOnlyNamespacesAndIsNotExhaustive()
    {
        var environment = Environment.Create(Guid.NewGuid(), "dev");
        _environments.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>()).Returns(environment);
        _envService.BuildVariablesForEnvironmentAsync(environment.Id, Arg.Any<CancellationToken>())
            .Returns([new EnvVar { Key = "REGION" }]);

        var result = await _sut.Handle(
            new GetTemplateSuggestionsQuery { ParentId = environment.Id, ParentType = EnvironmentVariableParentType.Environment },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.IsExhaustive.ShouldBeFalse();
        result.Value.Namespaces.ShouldBe(["env", "secrets", "runtime"]);
        result.Value.Variables.ShouldContain(v => v.Namespace == "env" && v.Key == "REGION");
    }

    [Test]
    public async Task Handle_UnknownService_ReturnsFailure()
    {
        _services.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Service?)null);

        var result = await _sut.Handle(
            new GetTemplateSuggestionsQuery { ParentId = Guid.NewGuid(), ParentType = EnvironmentVariableParentType.Service },
            CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }
}
