using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.Manifests.EventHandlers;
using Haven.Domain.Aggregates;
using Haven.Domain.Events;
using NSubstitute;
using Shouldly;

namespace Haven.Application.Tests.Features.Manifests.EventHandlers;

[Category("Unit")]
public sealed class WriteProjectOnManifestDirtyEventHandlerTests
{
    private WriteProjectOnManifestDirtyEventHandler _sut = null!;
    private IManifestSerializer<Project> _serializer = null!;
    private IProjectRepository _repository = null!;
    private CancellationToken _cancellationToken;

    [SetUp]
    public void Setup()
    {
        _serializer = Substitute.For<IManifestSerializer<Project>>();
        _repository = Substitute.For<IProjectRepository>();
        _sut = new WriteProjectOnManifestDirtyEventHandler(_serializer, _repository);
        _cancellationToken = CancellationToken.None;
    }

    [Test]
    public async Task Handle_ShouldNotWriteProjects_WhenNoProjectsExist()
    {
        var notification = new ManifestDirtyEvent();
        var emptyResult = new PagedResult<Project>([], 0, 1, 10);

        _repository.GetPagedAsync(1, 10, _cancellationToken).Returns(emptyResult);

        await _sut.Handle(notification, _cancellationToken);

        await _repository.Received(1).GetPagedAsync(1, 10, _cancellationToken);
        await _serializer.DidNotReceive().WriteAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ShouldWriteAllProjects_WhenSinglePageExists()
    {
        var notification = new ManifestDirtyEvent();
        var projects = new[] { CreateProject("project1"), CreateProject("project2"), CreateProject("project3") };
        var result = new PagedResult<Project>(projects, 3, 1, 10);

        _repository.GetPagedAsync(1, 10, _cancellationToken).Returns(result);

        await _sut.Handle(notification, _cancellationToken);

        await _repository.Received(1).GetPagedAsync(1, 10, _cancellationToken);
        await _serializer.Received(1).WriteAsync(projects[0], _cancellationToken);
        await _serializer.Received(1).WriteAsync(projects[1], _cancellationToken);
        await _serializer.Received(1).WriteAsync(projects[2], _cancellationToken);
    }

    [Test]
    public async Task Handle_ShouldPassCancellationTokenToRepository()
    {
        var notification = new ManifestDirtyEvent();
        var result = new PagedResult<Project>([], 0, 1, 10);
        var cts = new CancellationTokenSource();

        _repository.GetPagedAsync(1, 10, cts.Token).Returns(result);

        await _sut.Handle(notification, cts.Token);

        await _repository.Received(1).GetPagedAsync(1, 10, cts.Token);
    }

    [Test]
    public async Task Handle_ShouldPassCancellationTokenToSerializer()
    {
        var notification = new ManifestDirtyEvent();
        var project = CreateProject("project1");
        var result = new PagedResult<Project>([project], 1, 1, 10);
        var cts = new CancellationTokenSource();

        _repository.GetPagedAsync(1, 10, cts.Token).Returns(result);

        await _sut.Handle(notification, cts.Token);

        await _serializer.Received(1).WriteAsync(project, cts.Token);
    }

    [Test]
    public async Task Handle_ShouldPropagateCancellation_FromRepository()
    {
        var notification = new ManifestDirtyEvent();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _repository.GetPagedAsync(1, 10, cts.Token)
            .Returns(Task.FromException<PagedResult<Project>>(new OperationCanceledException()));

        var act = async () => await _sut.Handle(notification, cts.Token);

        await act.ShouldThrowAsync<OperationCanceledException>();
    }

    [Test]
    public async Task Handle_ShouldPropagateCancellation_FromSerializer()
    {
        var notification = new ManifestDirtyEvent();
        var project = CreateProject("project1");
        var result = new PagedResult<Project>([project], 1, 1, 10);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _repository.GetPagedAsync(1, 10, cts.Token).Returns(result);
        _serializer.WriteAsync(project, cts.Token)
            .Returns(Task.FromException(new OperationCanceledException()));

        var act = async () => await _sut.Handle(notification, cts.Token);

        await act.ShouldThrowAsync<OperationCanceledException>();
    }

    [Test]
    public async Task Handle_ShouldThrowWhenRepositoryThrows()
    {
        var notification = new ManifestDirtyEvent();
        var exception = new InvalidOperationException("Database error");

        _repository.GetPagedAsync(1, 10, _cancellationToken)
            .Returns(Task.FromException<PagedResult<Project>>(exception));

        var act = async () => await _sut.Handle(notification, _cancellationToken);

        await act.ShouldThrowAsync<InvalidOperationException>();
    }

    [Test]
    public async Task Handle_ShouldThrowWhenSerializerThrows()
    {
        var notification = new ManifestDirtyEvent();
        var project = CreateProject("project1");
        var result = new PagedResult<Project>([project], 1, 1, 10);
        var exception = new InvalidOperationException("Serialization error");

        _repository.GetPagedAsync(1, 10, _cancellationToken).Returns(result);
        _serializer.WriteAsync(project, _cancellationToken)
            .Returns(Task.FromException(exception));

        var act = async () => await _sut.Handle(notification, _cancellationToken);

        await act.ShouldThrowAsync<InvalidOperationException>();
    }

    [Test]
    public async Task Handle_ShouldWriteExactly10ProjectsPerPage_WhenPageSizeIs10()
    {
        var notification = new ManifestDirtyEvent();
        var projects = Enumerable.Range(1, 10)
            .Select(i => CreateProject($"project{i}"))
            .ToList();
        var result = new PagedResult<Project>(projects, 10, 1, 10);

        _repository.GetPagedAsync(1, 10, _cancellationToken).Returns(result);

        await _sut.Handle(notification, _cancellationToken);

        await _serializer.Received(10).WriteAsync(Arg.Any<Project>(), _cancellationToken);
    }

    [Test]
    public async Task Handle_ShouldGetFirstPage_WithPageSizeOf10()
    {
        var notification = new ManifestDirtyEvent();
        var result = new PagedResult<Project>([], 0, 1, 10);

        _repository.GetPagedAsync(1, 10, _cancellationToken).Returns(result);

        await _sut.Handle(notification, _cancellationToken);

        await _repository.Received(1).GetPagedAsync(1, 10, _cancellationToken);
    }

    [Test]
    public async Task Handle_ShouldWriteAllProjectsFromMultiplePages()
    {
        var notification = new ManifestDirtyEvent();
        var page1Projects = Enumerable.Range(1, 10)
            .Select(i => CreateProject($"project{i}"))
            .ToList();
        var page2Projects = Enumerable.Range(11, 6)
            .Select(i => CreateProject($"project{i}"))
            .ToList();

        var page1Result = new PagedResult<Project>(page1Projects, 16, 1, 10);
        var page2Result = new PagedResult<Project>(page2Projects, 16, 2, 10);

        _repository.GetPagedAsync(1, 10, _cancellationToken).Returns(page1Result);
        _repository.GetPagedAsync(2, 10, _cancellationToken).Returns(page2Result);

        await _sut.Handle(notification, _cancellationToken);

        // Should fetch page 1
        await _repository.Received(1).GetPagedAsync(1, 10, _cancellationToken);

        // Should fetch page 2 if handler implements pagination correctly
        await _repository.Received(1).GetPagedAsync(2, 10, _cancellationToken);

        // Should write all 16 projects
        await _serializer.Received(16).WriteAsync(Arg.Any<Project>(), _cancellationToken);
    }

    private static Project CreateProject(string name)
    {
        return Project.Reconstitute(Guid.NewGuid(), name, null);
    }
}
