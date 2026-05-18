using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.Manifests.EventHandlers;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Events;
using Haven.Domain.ValueObjects;
using NSubstitute;
using Shouldly;
using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Application.Tests.Features.Manifests.EventHandlers;

[Category("Unit")]
public sealed class WriteServiceOnManifestDirtyEventHandlerTests
{
    private WriteServiceOnManifestDirtyEventHandler _sut = null!;
    private IManifestSerializer _serializer = null!;
    private IProjectRepository _projectRepository = null!;
    private IEnvironmentRepository _environmentRepository = null!;
    private IServiceRepository _serviceRepository = null!;
    private CancellationToken _cancellationToken;

    [SetUp]
    public void Setup()
    {
        _serializer = Substitute.For<IManifestSerializer>();
        _projectRepository = Substitute.For<IProjectRepository>();
        _environmentRepository = Substitute.For<IEnvironmentRepository>();
        _serviceRepository = Substitute.For<IServiceRepository>();
        _sut = new WriteServiceOnManifestDirtyEventHandler(
            _serializer,
            _projectRepository,
            _environmentRepository,
            _serviceRepository);
        _cancellationToken = CancellationToken.None;
    }

    [Test]
    public async Task Handle_ShouldNotWriteServices_WhenNoProjectsExist()
    {
        var notification = new ManifestDirtyEvent();
        var emptyResult = new PagedResult<Project>([], 0, 1, 10);

        _projectRepository.GetPagedAsync(1, 10, _cancellationToken).Returns(emptyResult);

        await _sut.Handle(notification, _cancellationToken);

        await _projectRepository.Received(1).GetPagedAsync(1, 10, _cancellationToken);
        await _serializer.DidNotReceive().WriteServiceAsync(
            Arg.Any<Project>(),
            Arg.Any<Environment>(),
            Arg.Any<Service>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ShouldNotWriteServices_WhenProjectsHaveNoEnvironments()
    {
        var notification = new ManifestDirtyEvent();
        var projects = new[] { CreateProject("project1") };
        var result = new PagedResult<Project>(projects, 1, 1, 10);

        _projectRepository.GetPagedAsync(1, 10, _cancellationToken).Returns(result);
        _environmentRepository.GetByProjectIdAsync(Arg.Any<Guid>(), _cancellationToken).Returns(new List<Environment>());

        await _sut.Handle(notification, _cancellationToken);

        await _serializer.DidNotReceive().WriteServiceAsync(
            Arg.Any<Project>(),
            Arg.Any<Environment>(),
            Arg.Any<Service>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ShouldNotWriteServices_WhenEnvironmentsHaveNoServices()
    {
        var notification = new ManifestDirtyEvent();
        var project = CreateProject("project1");
        var projects = new[] { project };
        var result = new PagedResult<Project>(projects, 1, 1, 10);
        var environment = CreateEnvironment("dev", project);

        _projectRepository.GetPagedAsync(1, 10, _cancellationToken).Returns(result);
        _environmentRepository.GetByProjectIdAsync(project.Id, _cancellationToken).Returns(new[] { environment });
        _serviceRepository.GetByEnvironmentIdAsync(Arg.Any<Guid>(), _cancellationToken).Returns(new List<Service>());

        await _sut.Handle(notification, _cancellationToken);

        await _serializer.DidNotReceive().WriteServiceAsync(
            Arg.Any<Project>(),
            Arg.Any<Environment>(),
            Arg.Any<Service>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ShouldWriteAllServices_WhenEnvironmentsHaveServices()
    {
        var notification = new ManifestDirtyEvent();
        var project = CreateProject("project1");
        var projects = new[] { project };
        var result = new PagedResult<Project>(projects, 1, 1, 10);
        var environment = CreateEnvironment("dev", project);
        var services = new[] { CreateService("api"), CreateService("web") };

        _projectRepository.GetPagedAsync(1, 10, _cancellationToken).Returns(result);
        _environmentRepository.GetByProjectIdAsync(project.Id, _cancellationToken).Returns(new[] { environment });
        _serviceRepository.GetByEnvironmentIdAsync(environment.Id, _cancellationToken).Returns(services);

        await _sut.Handle(notification, _cancellationToken);

        await _serializer.Received(1).WriteServiceAsync(project, environment, services[0], _cancellationToken);
        await _serializer.Received(1).WriteServiceAsync(project, environment, services[1], _cancellationToken);
    }

    [Test]
    public async Task Handle_ShouldWriteServicesFromMultipleEnvironments()
    {
        var notification = new ManifestDirtyEvent();
        var project = CreateProject("project1");
        var projects = new[] { project };
        var result = new PagedResult<Project>(projects, 1, 1, 10);
        var env1 = CreateEnvironment("dev", project);
        var env2 = CreateEnvironment("staging", project);
        var service1 = CreateService("api");
        var service2 = CreateService("web");
        var service3 = CreateService("db");

        _projectRepository.GetPagedAsync(1, 10, _cancellationToken).Returns(result);
        _environmentRepository.GetByProjectIdAsync(project.Id, _cancellationToken).Returns(new[] { env1, env2 });
        _serviceRepository.GetByEnvironmentIdAsync(env1.Id, _cancellationToken).Returns(new[] { service1, service2 });
        _serviceRepository.GetByEnvironmentIdAsync(env2.Id, _cancellationToken).Returns(new[] { service3 });

        await _sut.Handle(notification, _cancellationToken);

        await _serializer.Received(1).WriteServiceAsync(project, env1, service1, _cancellationToken);
        await _serializer.Received(1).WriteServiceAsync(project, env1, service2, _cancellationToken);
        await _serializer.Received(1).WriteServiceAsync(project, env2, service3, _cancellationToken);
    }

    [Test]
    public async Task Handle_ShouldWriteServicesFromMultipleProjects()
    {
        var notification = new ManifestDirtyEvent();
        var project1 = CreateProject("project1");
        var project2 = CreateProject("project2");
        var projects = new[] { project1, project2 };
        var result = new PagedResult<Project>(projects, 2, 1, 10);
        var env1 = CreateEnvironment("dev", project1);
        var env2 = CreateEnvironment("dev", project2);
        var service1 = CreateService("api");
        var service2 = CreateService("web");

        _projectRepository.GetPagedAsync(1, 10, _cancellationToken).Returns(result);
        _environmentRepository.GetByProjectIdAsync(project1.Id, _cancellationToken).Returns(new[] { env1 });
        _environmentRepository.GetByProjectIdAsync(project2.Id, _cancellationToken).Returns(new[] { env2 });
        _serviceRepository.GetByEnvironmentIdAsync(env1.Id, _cancellationToken).Returns(new[] { service1 });
        _serviceRepository.GetByEnvironmentIdAsync(env2.Id, _cancellationToken).Returns(new[] { service2 });

        await _sut.Handle(notification, _cancellationToken);

        await _serializer.Received(1).WriteServiceAsync(project1, env1, service1, _cancellationToken);
        await _serializer.Received(1).WriteServiceAsync(project2, env2, service2, _cancellationToken);
    }

    [Test]
    public async Task Handle_ShouldHandleMultiplePages_OfProjects()
    {
        var notification = new ManifestDirtyEvent();
        var project1 = CreateProject("project1");
        var project2 = CreateProject("project2");
        var page1Result = new PagedResult<Project>(new[] { project1 }, 11, 1, 10);
        var page2Result = new PagedResult<Project>(new[] { project2 }, 11, 2, 10);
        var env1 = CreateEnvironment("dev", project1);
        var env2 = CreateEnvironment("dev", project2);
        var service1 = CreateService("api");
        var service2 = CreateService("web");

        _projectRepository.GetPagedAsync(1, 10, _cancellationToken).Returns(page1Result);
        _projectRepository.GetPagedAsync(2, 10, _cancellationToken).Returns(page2Result);
        _environmentRepository.GetByProjectIdAsync(project1.Id, _cancellationToken).Returns(new[] { env1 });
        _environmentRepository.GetByProjectIdAsync(project2.Id, _cancellationToken).Returns(new[] { env2 });
        _serviceRepository.GetByEnvironmentIdAsync(env1.Id, _cancellationToken).Returns(new[] { service1 });
        _serviceRepository.GetByEnvironmentIdAsync(env2.Id, _cancellationToken).Returns(new[] { service2 });

        await _sut.Handle(notification, _cancellationToken);

        await _projectRepository.Received(1).GetPagedAsync(1, 10, _cancellationToken);
        await _projectRepository.Received(1).GetPagedAsync(2, 10, _cancellationToken);
        await _serializer.Received(2).WriteServiceAsync(Arg.Any<Project>(), Arg.Any<Environment>(), Arg.Any<Service>(), _cancellationToken);
    }

    [Test]
    public async Task Handle_ShouldPassCancellationToken()
    {
        var notification = new ManifestDirtyEvent();
        var project = CreateProject("project1");
        var projects = new[] { project };
        var result = new PagedResult<Project>(projects, 1, 1, 10);
        var environment = CreateEnvironment("dev", project);
        var service = CreateService("api");
        var cts = new CancellationTokenSource();

        _projectRepository.GetPagedAsync(1, 10, cts.Token).Returns(result);
        _environmentRepository.GetByProjectIdAsync(project.Id, cts.Token).Returns(new[] { environment });
        _serviceRepository.GetByEnvironmentIdAsync(environment.Id, cts.Token).Returns(new[] { service });

        await _sut.Handle(notification, cts.Token);

        await _projectRepository.Received(1).GetPagedAsync(1, 10, cts.Token);
        await _environmentRepository.Received(1).GetByProjectIdAsync(project.Id, cts.Token);
        await _serviceRepository.Received(1).GetByEnvironmentIdAsync(environment.Id, cts.Token);
        await _serializer.Received(1).WriteServiceAsync(project, environment, service, cts.Token);
    }

    [Test]
    public async Task Handle_ShouldPropagateCancellation_FromProjectRepository()
    {
        var notification = new ManifestDirtyEvent();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _projectRepository.GetPagedAsync(1, 10, cts.Token)
            .Returns(Task.FromException<PagedResult<Project>>(new OperationCanceledException()));

        var act = async () => await _sut.Handle(notification, cts.Token);

        await act.ShouldThrowAsync<OperationCanceledException>();
    }

    [Test]
    public async Task Handle_ShouldPropagateCancellation_FromEnvironmentRepository()
    {
        var notification = new ManifestDirtyEvent();
        var project = CreateProject("project1");
        var projects = new[] { project };
        var result = new PagedResult<Project>(projects, 1, 1, 10);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _projectRepository.GetPagedAsync(1, 10, cts.Token).Returns(result);
        _environmentRepository.GetByProjectIdAsync(project.Id, cts.Token)
            .Returns(Task.FromException<IReadOnlyList<Environment>>(new OperationCanceledException()));

        var act = async () => await _sut.Handle(notification, cts.Token);

        await act.ShouldThrowAsync<OperationCanceledException>();
    }

    [Test]
    public async Task Handle_ShouldPropagateCancellation_FromServiceRepository()
    {
        var notification = new ManifestDirtyEvent();
        var project = CreateProject("project1");
        var projects = new[] { project };
        var result = new PagedResult<Project>(projects, 1, 1, 10);
        var environment = CreateEnvironment("dev", project);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _projectRepository.GetPagedAsync(1, 10, cts.Token).Returns(result);
        _environmentRepository.GetByProjectIdAsync(project.Id, cts.Token).Returns(new[] { environment });
        _serviceRepository.GetByEnvironmentIdAsync(environment.Id, cts.Token)
            .Returns(Task.FromException<IReadOnlyList<Service>>(new OperationCanceledException()));

        var act = async () => await _sut.Handle(notification, cts.Token);

        await act.ShouldThrowAsync<OperationCanceledException>();
    }

    [Test]
    public async Task Handle_ShouldPropagateCancellation_FromSerializer()
    {
        var notification = new ManifestDirtyEvent();
        var project = CreateProject("project1");
        var projects = new[] { project };
        var result = new PagedResult<Project>(projects, 1, 1, 10);
        var environment = CreateEnvironment("dev", project);
        var service = CreateService("api");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _projectRepository.GetPagedAsync(1, 10, cts.Token).Returns(result);
        _environmentRepository.GetByProjectIdAsync(project.Id, cts.Token).Returns(new[] { environment });
        _serviceRepository.GetByEnvironmentIdAsync(environment.Id, cts.Token).Returns(new[] { service });
        _serializer.WriteServiceAsync(project, environment, service, cts.Token)
            .Returns(Task.FromException(new OperationCanceledException()));

        var act = async () => await _sut.Handle(notification, cts.Token);

        await act.ShouldThrowAsync<OperationCanceledException>();
    }

    [Test]
    public async Task Handle_ShouldThrowWhenProjectRepositoryThrows()
    {
        var notification = new ManifestDirtyEvent();
        var exception = new InvalidOperationException("Database error");

        _projectRepository.GetPagedAsync(1, 10, _cancellationToken)
            .Returns(Task.FromException<PagedResult<Project>>(exception));

        var act = async () => await _sut.Handle(notification, _cancellationToken);

        await act.ShouldThrowAsync<InvalidOperationException>();
    }

    [Test]
    public async Task Handle_ShouldThrowWhenEnvironmentRepositoryThrows()
    {
        var notification = new ManifestDirtyEvent();
        var project = CreateProject("project1");
        var projects = new[] { project };
        var result = new PagedResult<Project>(projects, 1, 1, 10);
        var exception = new InvalidOperationException("Database error");

        _projectRepository.GetPagedAsync(1, 10, _cancellationToken).Returns(result);
        _environmentRepository.GetByProjectIdAsync(project.Id, _cancellationToken)
            .Returns(Task.FromException<IReadOnlyList<Environment>>(exception));

        var act = async () => await _sut.Handle(notification, _cancellationToken);

        await act.ShouldThrowAsync<InvalidOperationException>();
    }

    [Test]
    public async Task Handle_ShouldThrowWhenServiceRepositoryThrows()
    {
        var notification = new ManifestDirtyEvent();
        var project = CreateProject("project1");
        var projects = new[] { project };
        var result = new PagedResult<Project>(projects, 1, 1, 10);
        var environment = CreateEnvironment("dev", project);
        var exception = new InvalidOperationException("Database error");

        _projectRepository.GetPagedAsync(1, 10, _cancellationToken).Returns(result);
        _environmentRepository.GetByProjectIdAsync(project.Id, _cancellationToken).Returns(new[] { environment });
        _serviceRepository.GetByEnvironmentIdAsync(environment.Id, _cancellationToken)
            .Returns(Task.FromException<IReadOnlyList<Service>>(exception));

        var act = async () => await _sut.Handle(notification, _cancellationToken);

        await act.ShouldThrowAsync<InvalidOperationException>();
    }

    [Test]
    public async Task Handle_ShouldThrowWhenSerializerThrows()
    {
        var notification = new ManifestDirtyEvent();
        var project = CreateProject("project1");
        var projects = new[] { project };
        var result = new PagedResult<Project>(projects, 1, 1, 10);
        var environment = CreateEnvironment("dev", project);
        var service = CreateService("api");
        var exception = new InvalidOperationException("Serialization error");

        _projectRepository.GetPagedAsync(1, 10, _cancellationToken).Returns(result);
        _environmentRepository.GetByProjectIdAsync(project.Id, _cancellationToken).Returns(new[] { environment });
        _serviceRepository.GetByEnvironmentIdAsync(environment.Id, _cancellationToken).Returns(new[] { service });
        _serializer.WriteServiceAsync(project, environment, service, _cancellationToken)
            .Returns(Task.FromException(exception));

        var act = async () => await _sut.Handle(notification, _cancellationToken);

        await act.ShouldThrowAsync<InvalidOperationException>();
    }

    private static Project CreateProject(string name) =>
        Project.Reconstitute(Guid.NewGuid(), name, null);

    private static Environment CreateEnvironment(string name, Project project) =>
        Environment.Reconstitute(
            Guid.NewGuid(),
            project.Id,
            name,
            null,
            $"haven_{project.Id:N}_{name.ToLower()}",
            project: project);

    private static Service CreateService(string name) =>
        Service.Reconstitute(
            Guid.NewGuid(),
            Guid.NewGuid(),
            name,
            ServiceType.DockerImage,
            ExposureMode.External,
            ServiceStatus.Stopped,
            DateTime.UtcNow,
            DateTime.UtcNow,
            sourceConfig: new DockerConfig() { Image = "haventest"});
}
