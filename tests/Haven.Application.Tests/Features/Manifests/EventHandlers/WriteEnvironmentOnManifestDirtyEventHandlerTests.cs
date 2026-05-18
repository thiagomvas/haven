using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.Manifests.EventHandlers;
using Haven.Domain.Aggregates;
using Haven.Domain.Events;
using NSubstitute;
using Shouldly;
using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Application.Tests.Features.Manifests.EventHandlers;

[Category("Unit")]
public sealed class WriteEnvironmentOnManifestDirtyEventHandlerTests
{
    private WriteEnvironmentOnManifestDirtyEventHandler _sut = null!;
    private IManifestSerializer _serializer = null!;
    private IProjectRepository _projectRepository = null!;
    private IEnvironmentRepository _environmentRepository = null!;
    private CancellationToken _cancellationToken;

    [SetUp]
    public void Setup()
    {
        _serializer = Substitute.For<IManifestSerializer>();
        _projectRepository = Substitute.For<IProjectRepository>();
        _environmentRepository = Substitute.For<IEnvironmentRepository>();
        _sut = new WriteEnvironmentOnManifestDirtyEventHandler(_serializer, _projectRepository, _environmentRepository);
        _cancellationToken = CancellationToken.None;
    }

    [Test]
    public async Task Handle_ShouldNotWriteEnvironments_WhenNoProjectsExist()
    {
        var notification = new ManifestDirtyEvent();
        var emptyResult = new PagedResult<Project>([], 0, 1, 10);

        _projectRepository.GetPagedAsync(1, 10, _cancellationToken).Returns(emptyResult);

        await _sut.Handle(notification, _cancellationToken);

        await _projectRepository.Received(1).GetPagedAsync(1, 10, _cancellationToken);
        await _serializer.DidNotReceive().WriteEnvironmentAsync(Arg.Any<Project>(), Arg.Any<Environment>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ShouldNotWriteEnvironments_WhenProjectsHaveNoEnvironments()
    {
        var notification = new ManifestDirtyEvent();
        var projects = new[] { CreateProject("project1"), CreateProject("project2") };
        var result = new PagedResult<Project>(projects, 2, 1, 10);

        _projectRepository.GetPagedAsync(1, 10, _cancellationToken).Returns(result);
        _environmentRepository.GetByProjectIdAsync(Arg.Any<Guid>(), _cancellationToken).Returns(new List<Environment>());

        await _sut.Handle(notification, _cancellationToken);

        await _projectRepository.Received(1).GetPagedAsync(1, 10, _cancellationToken);
        await _serializer.DidNotReceive().WriteEnvironmentAsync(Arg.Any<Project>(), Arg.Any<Environment>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ShouldWriteAllEnvironments_WhenProjectsHaveEnvironments()
    {
        var notification = new ManifestDirtyEvent();
        var project = CreateProject("project1");
        var projects = new[] { project };
        var result = new PagedResult<Project>(projects, 1, 1, 10);
        var environments = new[] { CreateEnvironment("dev", project), CreateEnvironment("staging", project) };

        _projectRepository.GetPagedAsync(1, 10, _cancellationToken).Returns(result);
        _environmentRepository.GetByProjectIdAsync(project.Id, _cancellationToken).Returns(environments);

        await _sut.Handle(notification, _cancellationToken);

        await _projectRepository.Received(1).GetPagedAsync(1, 10, _cancellationToken);
        await _environmentRepository.Received(1).GetByProjectIdAsync(project.Id, _cancellationToken);
        await _serializer.Received(1).WriteEnvironmentAsync(project, environments[0], _cancellationToken);
        await _serializer.Received(1).WriteEnvironmentAsync(project, environments[1], _cancellationToken);
    }

    [Test]
    public async Task Handle_ShouldWriteEnvironmentsFromMultipleProjects()
    {
        var notification = new ManifestDirtyEvent();
        var project1 = CreateProject("project1");
        var project2 = CreateProject("project2");
        var projects = new[] { project1, project2 };
        var result = new PagedResult<Project>(projects, 2, 1, 10);

        var env1 = CreateEnvironment("dev", project1);
        var env2 = CreateEnvironment("staging", project1);
        var env3 = CreateEnvironment("prod", project2);

        _projectRepository.GetPagedAsync(1, 10, _cancellationToken).Returns(result);
        _environmentRepository.GetByProjectIdAsync(project1.Id, _cancellationToken).Returns(new[] { env1, env2 });
        _environmentRepository.GetByProjectIdAsync(project2.Id, _cancellationToken).Returns(new[] { env3 });

        await _sut.Handle(notification, _cancellationToken);

        await _serializer.Received(1).WriteEnvironmentAsync(project1, env1, _cancellationToken);
        await _serializer.Received(1).WriteEnvironmentAsync(project1, env2, _cancellationToken);
        await _serializer.Received(1).WriteEnvironmentAsync(project2, env3, _cancellationToken);
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

        _projectRepository.GetPagedAsync(1, 10, _cancellationToken).Returns(page1Result);
        _projectRepository.GetPagedAsync(2, 10, _cancellationToken).Returns(page2Result);
        _environmentRepository.GetByProjectIdAsync(project1.Id, _cancellationToken).Returns(new[] { env1 });
        _environmentRepository.GetByProjectIdAsync(project2.Id, _cancellationToken).Returns(new[] { env2 });

        await _sut.Handle(notification, _cancellationToken);

        await _projectRepository.Received(1).GetPagedAsync(1, 10, _cancellationToken);
        await _projectRepository.Received(1).GetPagedAsync(2, 10, _cancellationToken);
        await _serializer.Received(2).WriteEnvironmentAsync(Arg.Any<Project>(), Arg.Any<Environment>(), _cancellationToken);
    }

    [Test]
    public async Task Handle_ShouldPassCancellationToken()
    {
        var notification = new ManifestDirtyEvent();
        var project = CreateProject("project1");
        var projects = new[] { project };
        var result = new PagedResult<Project>(projects, 1, 1, 10);
        var environment = CreateEnvironment("dev", project);
        var cts = new CancellationTokenSource();

        _projectRepository.GetPagedAsync(1, 10, cts.Token).Returns(result);
        _environmentRepository.GetByProjectIdAsync(project.Id, cts.Token).Returns(new[] { environment });

        await _sut.Handle(notification, cts.Token);

        await _projectRepository.Received(1).GetPagedAsync(1, 10, cts.Token);
        await _environmentRepository.Received(1).GetByProjectIdAsync(project.Id, cts.Token);
        await _serializer.Received(1).WriteEnvironmentAsync(project, environment, cts.Token);
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
    public async Task Handle_ShouldPropagateCancellation_FromSerializer()
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
        _serializer.WriteEnvironmentAsync(project, environment, cts.Token)
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
    public async Task Handle_ShouldThrowWhenSerializerThrows()
    {
        var notification = new ManifestDirtyEvent();
        var project = CreateProject("project1");
        var projects = new[] { project };
        var result = new PagedResult<Project>(projects, 1, 1, 10);
        var environment = CreateEnvironment("dev", project);
        var exception = new InvalidOperationException("Serialization error");

        _projectRepository.GetPagedAsync(1, 10, _cancellationToken).Returns(result);
        _environmentRepository.GetByProjectIdAsync(project.Id, _cancellationToken).Returns(new[] { environment });
        _serializer.WriteEnvironmentAsync(project, environment, _cancellationToken)
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
}
