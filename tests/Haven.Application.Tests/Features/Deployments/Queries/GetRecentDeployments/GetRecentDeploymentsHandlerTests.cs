using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.Deployments.Queries.GetRecentDeployments;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;

using NSubstitute;

using Shouldly;

namespace Haven.Application.Tests.Features.Deployments.Queries.GetRecentDeployments;

[Category("Unit")]
public sealed class GetRecentDeploymentsHandlerTests
{
    private IDeploymentRepository _repository;
    private GetRecentDeploymentsHandler _sut;

    [SetUp]
    public void Setup()
    {
        _repository = Substitute.For<IDeploymentRepository>();
        _sut = new GetRecentDeploymentsHandler(_repository);
    }

    [Test]
    public async Task Handle_ShouldMapDeploymentsWithServiceEnvironmentAndProject()
    {
        var project = Project.Create("shop");
        var environment = project.AddEnvironment("prod");
        var service = environment.AddService("api", ServiceType.DockerImage, ExposureMode.Internal);
        service.Environment = environment;
        environment.Project = project;
        var deployment = Deployment.Create(service.Id, "log.txt", "webhook");
        deployment.Service = service;
        _repository.GetRecentAsync(5, Arg.Any<CancellationToken>()).Returns([deployment]);

        var result = await _sut.Handle(new GetRecentDeploymentsQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var dto = result.Value.ShouldHaveSingleItem();
        dto.ServiceName.ShouldBe("api");
        dto.EnvironmentName.ShouldBe("prod");
        dto.ProjectName.ShouldBe("shop");
        dto.TriggeredBy.ShouldBe("webhook");
        dto.Status.ShouldBe(DeploymentStatus.Queued);
    }

    [Test]
    public async Task Handle_ShouldSkipDeploymentsWhoseServiceWasRemoved()
    {
        var orphan = Deployment.Create(Guid.NewGuid(), "log.txt");
        _repository.GetRecentAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([orphan]);

        var result = await _sut.Handle(new GetRecentDeploymentsQuery(), CancellationToken.None);

        result.Value.ShouldBeEmpty();
    }

    [Test]
    public async Task Handle_ShouldClampTheLimit()
    {
        _repository.GetRecentAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);

        await _sut.Handle(new GetRecentDeploymentsQuery { Limit = 10_000 }, CancellationToken.None);
        await _sut.Handle(new GetRecentDeploymentsQuery { Limit = 0 }, CancellationToken.None);

        await _repository.Received(1).GetRecentAsync(50, Arg.Any<CancellationToken>());
        await _repository.Received(1).GetRecentAsync(1, Arg.Any<CancellationToken>());
    }
}