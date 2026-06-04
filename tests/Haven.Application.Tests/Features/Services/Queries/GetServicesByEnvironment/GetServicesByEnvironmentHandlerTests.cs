using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.Services.Queries;
using Haven.Application.Features.Services.Queries.GetServicesByEnvironment;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Models;
using NSubstitute;
using Shouldly;
using Environment = Haven.Domain.Entities.Environment;


namespace Haven.Application.Tests.Features.Services.Queries.GetServicesByEnvironment;

[Category("Unit")]
public sealed class GetServicesByEnvironmentHandlerTests
{
    private IProjectRepository _projectRepository;
    private IEnvironmentRepository _environmentRepository;
    private IServiceRepository _serviceRepository;
    private GetServicesByEnvironmentHandler _sut;

    [SetUp]
    public void Setup()
    {
        _projectRepository = Substitute.For<IProjectRepository>();
        _environmentRepository = Substitute.For<IEnvironmentRepository>();
        _serviceRepository = Substitute.For<IServiceRepository>();
        _sut = new GetServicesByEnvironmentHandler(_projectRepository, _environmentRepository, _serviceRepository);
    }

    [Test]
    public async Task Handle_ShouldReturnFailure_WhenProjectDoesNotExist()
    {
        var query = CreateQuery(Guid.NewGuid(), Guid.NewGuid());
        _projectRepository.GetByIdAsync(query.ProjectId, Arg.Any<CancellationToken>())
            .Returns((Project?)null);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        await _environmentRepository.DidNotReceive()
            .GetByProjectIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ShouldReturnFailure_WhenEnvironmentDoesNotBelongToProject()
    {
        var project = Project.Create("test-project");
        var query = CreateQuery(project.Id, Guid.NewGuid());

        _projectRepository.GetByIdAsync(query.ProjectId, Arg.Any<CancellationToken>())
            .Returns(project);
        _environmentRepository.GetByProjectIdAsync(query.ProjectId, Arg.Any<CancellationToken>())
            .Returns(new List<Environment>() as IReadOnlyList<Environment>);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        await _serviceRepository.DidNotReceive()
            .GetByEnvironmentIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ShouldReturnEmptyList_WhenEnvironmentHasNoServices()
    {
        var project = Project.Create("test-project");
        var environment = project.AddEnvironment("staging");
        var query = CreateQuery(project.Id, environment.Id);

        _projectRepository.GetByIdAsync(query.ProjectId, Arg.Any<CancellationToken>())
            .Returns(project);
        _environmentRepository.GetByProjectIdAsync(query.ProjectId, Arg.Any<CancellationToken>())
            .Returns(new List<Environment> { environment } as IReadOnlyList<Environment>);
        _serviceRepository.GetByEnvironmentIdAsync(query.EnvironmentId, Arg.Any<CancellationToken>())
            .Returns(new List<Service>() as IReadOnlyList<Service>);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    [Test]
    public async Task Handle_ShouldReturnServiceDtos_MappedCorrectly()
    {
        var project = Project.Create("test-project");
        var environment = project.AddEnvironment("staging");
        var query = CreateQuery(project.Id, environment.Id);

        var services = new List<Service>
        {
            BuildService(environment.Id, "web", ServiceType.DockerImage, ExposureMode.External),
            BuildService(environment.Id, "worker", ServiceType.DockerImage, ExposureMode.None)
        };

        _projectRepository.GetByIdAsync(query.ProjectId, Arg.Any<CancellationToken>())
            .Returns(project);
        _environmentRepository.GetByProjectIdAsync(query.ProjectId, Arg.Any<CancellationToken>())
            .Returns(new List<Environment> { environment } as IReadOnlyList<Environment>);
        _serviceRepository.GetByEnvironmentIdAsync(query.EnvironmentId, Arg.Any<CancellationToken>())
            .Returns(services as IReadOnlyList<Service>);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(2);

        var webDto = result.Value.FirstOrDefault(s => s.Name == "web");
        webDto.ShouldNotBeNull();
        webDto.EnvironmentId.ShouldBe(environment.Id);
        webDto.Type.ShouldBe(ServiceType.DockerImage);
        webDto.ExposureMode.ShouldBe(ExposureMode.External);
        webDto.Status.ShouldBe(ServiceStatus.Stopped);
    }

    [Test]
    public async Task Handle_ShouldQueryServiceRepository_WithCorrectEnvironmentId()
    {
        var project = Project.Create("test-project");
        var environment = project.AddEnvironment("staging");
        var query = CreateQuery(project.Id, environment.Id);

        _projectRepository.GetByIdAsync(query.ProjectId, Arg.Any<CancellationToken>())
            .Returns(project);
        _environmentRepository.GetByProjectIdAsync(query.ProjectId, Arg.Any<CancellationToken>())
            .Returns(new List<Environment> { environment } as IReadOnlyList<Environment>);
        _serviceRepository.GetByEnvironmentIdAsync(query.EnvironmentId, Arg.Any<CancellationToken>())
            .Returns(new List<Service>() as IReadOnlyList<Service>);

        await _sut.Handle(query, CancellationToken.None);

        await _serviceRepository.Received(1)
            .GetByEnvironmentIdAsync(query.EnvironmentId, Arg.Any<CancellationToken>());
    }

    private static GetServicesByEnvironmentQuery CreateQuery(Guid projectId, Guid environmentId) => new()
    {
        ProjectId = projectId,
        EnvironmentId = environmentId
    };

    private static Service BuildService(Guid environmentId, string name, ServiceType type, ExposureMode mode)
    {
        var projectId = Guid.NewGuid();
        var project = Project.Reconstitute(
            projectId, "test-project", null, null,
            [new EnvironmentData(environmentId, projectId, "staging", null, null, $"haven-{projectId.ToString("N")[..8]}-staging")]);
        return project.AddService(environmentId, name, type, mode);
    }
}
