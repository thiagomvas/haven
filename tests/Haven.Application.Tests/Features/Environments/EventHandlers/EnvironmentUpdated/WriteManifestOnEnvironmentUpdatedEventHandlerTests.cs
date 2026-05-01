using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.Environments.Events;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Events;
using NSubstitute;
using Shouldly;
using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Application.Tests.Features.Environments.EventHandlers.EnvironmentUpdated;

[Category("Unit")]
public sealed class WriteManifestOnEnvironmentUpdatedEventHandlerTests
{
    private WriteManifestOnEnvironmentUpdatedEventHandler _sut = null!;
    private IEnvironmentRepository _repository = null!;
    private IManifestSerializer _serializer = null!;
    private CancellationToken _cancellationToken;

    [SetUp]
    public void Setup()
    {
        _repository = Substitute.For<IEnvironmentRepository>();
        _serializer = Substitute.For<IManifestSerializer>();
        _sut = new WriteManifestOnEnvironmentUpdatedEventHandler(_repository, _serializer);
        _cancellationToken = CancellationToken.None;
    }

    [Test]
    public async Task Handle_ShouldReturnEarly_WhenOldNameEqualsNewName()
    {
        var environmentId = Guid.NewGuid();
        var notification = new EnvironmentUpdatedEvent(environmentId, "dev", "dev");

        await _sut.Handle(notification, _cancellationToken);

        await _repository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _serializer.DidNotReceive().WriteEnvironmentAsync(
            Arg.Any<Project>(),
            Arg.Any<Environment>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ShouldReturnEarly_WhenEnvironmentIsNull()
    {
        var environmentId = Guid.NewGuid();
        var notification = new EnvironmentUpdatedEvent(environmentId, "old-env", "new-env");

        _repository.GetByIdAsync(environmentId, _cancellationToken).Returns((Environment?)null);

        await _sut.Handle(notification, _cancellationToken);

        await _repository.Received(1).GetByIdAsync(environmentId, _cancellationToken);
        await _serializer.DidNotReceive().RenameEnvironmentAsync(
            Arg.Any<Project>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        await _serializer.DidNotReceive().WriteEnvironmentAsync(
            Arg.Any<Project>(),
            Arg.Any<Environment>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ShouldReturnEarly_WhenEnvironmentProjectIsNull()
    {
        var environmentId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var notification = new EnvironmentUpdatedEvent(environmentId, "old-env", "new-env");

        var environment = Environment.Reconstitute(
            environmentId,
            projectId,
            "new-env",
            null,
            "haven_abc123_new_env",
            project: null);

        _repository.GetByIdAsync(environmentId, _cancellationToken).Returns(environment);

        await _sut.Handle(notification, _cancellationToken);

        await _repository.Received(1).GetByIdAsync(environmentId, _cancellationToken);
        await _serializer.DidNotReceive().RenameEnvironmentAsync(
            Arg.Any<Project>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        await _serializer.DidNotReceive().WriteEnvironmentAsync(
            Arg.Any<Project>(),
            Arg.Any<Environment>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ShouldNotRename_WhenOldNameIsNullOrWhitespace()
    {
        var environmentId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var project = CreateProject(projectId);
        var environment = CreateEnvironment(environmentId, projectId, "new-env", project);

        var notification = new EnvironmentUpdatedEvent(environmentId, "   ", "new-env");

        _repository.GetByIdAsync(environmentId, _cancellationToken).Returns(environment);

        await _sut.Handle(notification, _cancellationToken);

        await _serializer.DidNotReceive().RenameEnvironmentAsync(
            Arg.Any<Project>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        await _serializer.Received(1).WriteEnvironmentAsync(project, environment, _cancellationToken);
    }

    [Test]
    public async Task Handle_ShouldNotRename_WhenNewNameIsNullOrWhitespace()
    {
        var environmentId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var project = CreateProject(projectId);
        var environment = CreateEnvironment(environmentId, projectId, "", project);

        var notification = new EnvironmentUpdatedEvent(environmentId, "old-env", "   ");

        _repository.GetByIdAsync(environmentId, _cancellationToken).Returns(environment);

        await _sut.Handle(notification, _cancellationToken);

        await _serializer.DidNotReceive().RenameEnvironmentAsync(
            Arg.Any<Project>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        await _serializer.Received(1).WriteEnvironmentAsync(project, environment, _cancellationToken);
    }

    [Test]
    public async Task Handle_ShouldCallRenameEnvironment_WhenNameChanged()
    {
        var environmentId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var oldName = "old-env";
        var newName = "new-env";
        var project = CreateProject(projectId);
        var environment = CreateEnvironment(environmentId, projectId, newName, project);

        var notification = new EnvironmentUpdatedEvent(environmentId, oldName, newName);

        _repository.GetByIdAsync(environmentId, _cancellationToken).Returns(environment);

        await _sut.Handle(notification, _cancellationToken);

        await _serializer.Received(1).RenameEnvironmentAsync(
            project,
            oldName,
            newName,
            _cancellationToken);
        await _serializer.Received(1).WriteEnvironmentAsync(project, environment, _cancellationToken);
    }

    [Test]
    public async Task Handle_ShouldCallWriteEnvironmentAsync_AlwaysAfterValidation()
    {
        var environmentId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var project = CreateProject(projectId);
        var environment = CreateEnvironment(environmentId, projectId, "staging", project);

        var notification = new EnvironmentUpdatedEvent(environmentId, "dev", "staging");

        _repository.GetByIdAsync(environmentId, _cancellationToken).Returns(environment);

        await _sut.Handle(notification, _cancellationToken);

        await _serializer.Received(1).WriteEnvironmentAsync(project, environment, _cancellationToken);
    }

    [Test]
    public async Task Handle_ShouldNotRename_WhenOldNameEqualsNewName()
    {
        var environmentId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var sameName = "production";
        var project = CreateProject(projectId);
        var environment = CreateEnvironment(environmentId, projectId, sameName, project);

        var notification = new EnvironmentUpdatedEvent(environmentId, sameName, sameName);

        _repository.GetByIdAsync(environmentId, _cancellationToken).Returns(environment);

        // Should return early, so repository is not called
        await _sut.Handle(notification, _cancellationToken);

        await _repository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ShouldPropagateCancellation_FromRepository()
    {
        var environmentId = Guid.NewGuid();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var notification = new EnvironmentUpdatedEvent(environmentId, "old", "new");

        _repository.GetByIdAsync(environmentId, cts.Token)
            .Returns(Task.FromException<Environment?>(new OperationCanceledException()));

        var act = async () => await _sut.Handle(notification, cts.Token);

        await act.ShouldThrowAsync<OperationCanceledException>();
    }

    [Test]
    public async Task Handle_ShouldPropagateCancellation_FromSerializer()
    {
        var environmentId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var project = CreateProject(projectId);
        var environment = CreateEnvironment(environmentId, projectId, "staging", project);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        var notification = new EnvironmentUpdatedEvent(environmentId, "dev", "staging");

        _repository.GetByIdAsync(environmentId, cts.Token).Returns(environment);
        _serializer.RenameEnvironmentAsync(Arg.Any<Project>(), Arg.Any<string>(), Arg.Any<string>(), cts.Token)
            .Returns(Task.FromException(new OperationCanceledException()));

        var act = async () => await _sut.Handle(notification, cts.Token);

        await act.ShouldThrowAsync<OperationCanceledException>();
    }

    private static Project CreateProject(Guid projectId)
    {
        return Project.Reconstitute(projectId, "test-project", null);
    }

    private static Environment CreateEnvironment(Guid environmentId, Guid projectId, string name, Project project)
    {
        return Environment.Reconstitute(
            environmentId,
            projectId,
            name,
            "Test environment",
            $"haven_{projectId:N}_{name.ToLower()}",
            project: project);
    }
}
