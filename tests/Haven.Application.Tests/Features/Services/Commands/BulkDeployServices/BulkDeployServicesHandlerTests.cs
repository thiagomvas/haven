using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.Services.Commands.BulkDeployServices;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;

using NSubstitute;

using Shouldly;

namespace Haven.Application.Tests.Features.Services.Commands.BulkDeployServices;

[Category("Unit")]
public sealed class BulkDeployServicesHandlerTests
{
    private IProjectRepository _projectRepository;
    private IDeploymentJobEnqueuer _deploymentJobEnqueuer;
    private BulkDeployServicesHandler _sut;

    [SetUp]
    public void Setup()
    {
        _projectRepository = Substitute.For<IProjectRepository>();
        _deploymentJobEnqueuer = Substitute.For<IDeploymentJobEnqueuer>();
        _sut = new BulkDeployServicesHandler(_projectRepository, _deploymentJobEnqueuer);
    }

    [Test]
    public async Task Handle_ShouldReturnFailure_WhenProjectDoesNotExist()
    {
        var command = CreateCommand([Guid.NewGuid()]);
        _projectRepository.GetByIdAsync(command.ProjectId, Arg.Any<CancellationToken>())
            .Returns((Project?)null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_ShouldReturnFailure_WhenEnvironmentDoesNotExist()
    {
        var command = CreateCommand([Guid.NewGuid()]);
        var project = Project.Create("test-project");
        _projectRepository.GetByIdAsync(command.ProjectId, Arg.Any<CancellationToken>())
            .Returns(project);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_ShouldReturnSuccessWithAllSucceeded_WhenAllServicesExist()
    {
        var project = Project.Create("test-project");
        var environment = project.AddEnvironment("staging");
        var service1 = project.AddService(environment.Id, "web", ServiceType.DockerImage, ExposureMode.External,
            null, new DockerConfig { Image = "nginx" });
        var service2 = project.AddService(environment.Id, "api", ServiceType.DockerImage, ExposureMode.External,
            null, new DockerConfig { Image = "api" });
        var command = CreateCommand([service1.Id, service2.Id]);
        command.EnvironmentId = environment.Id;

        _projectRepository.GetByIdAsync(command.ProjectId, Arg.Any<CancellationToken>())
            .Returns(project);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.SucceededCount.ShouldBe(2);
        result.Value.FailedCount.ShouldBe(0);
        _deploymentJobEnqueuer.Received(1).EnqueueDeployment(command.ProjectId, command.EnvironmentId, service1.Id);
        _deploymentJobEnqueuer.Received(1).EnqueueDeployment(command.ProjectId, command.EnvironmentId, service2.Id);
    }

    [Test]
    public async Task Handle_ShouldReportPartialFailure_WhenSomeServicesDoNotExist()
    {
        var project = Project.Create("test-project");
        var environment = project.AddEnvironment("staging");
        var service = project.AddService(environment.Id, "web", ServiceType.DockerImage, ExposureMode.External,
            null, new DockerConfig { Image = "nginx" });
        var missingServiceId = Guid.NewGuid();
        var command = CreateCommand([service.Id, missingServiceId]);
        command.EnvironmentId = environment.Id;

        _projectRepository.GetByIdAsync(command.ProjectId, Arg.Any<CancellationToken>())
            .Returns(project);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.SucceededCount.ShouldBe(1);
        result.Value.FailedCount.ShouldBe(1);
        result.Value.Results.Single(r => r.ServiceId == missingServiceId).Success.ShouldBeFalse();
        result.Value.Results.Single(r => r.ServiceId == service.Id).Success.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_ShouldReturnSuccessWithAllFailed_WhenNoServicesExist()
    {
        var project = Project.Create("test-project");
        var environment = project.AddEnvironment("staging");
        var command = CreateCommand([Guid.NewGuid(), Guid.NewGuid()]);
        command.EnvironmentId = environment.Id;

        _projectRepository.GetByIdAsync(command.ProjectId, Arg.Any<CancellationToken>())
            .Returns(project);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.FailedCount.ShouldBe(2);
        result.Value.SucceededCount.ShouldBe(0);
    }

    private static BulkDeployServicesCommand CreateCommand(IReadOnlyList<Guid> serviceIds) => new()
    {
        ProjectId = Guid.NewGuid(),
        EnvironmentId = Guid.NewGuid(),
        ServiceIds = serviceIds,
    };
}
