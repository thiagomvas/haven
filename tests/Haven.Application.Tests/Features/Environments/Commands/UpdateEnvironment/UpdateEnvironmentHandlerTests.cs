using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.Environments.Commands.UpdateEnvironment;
using Haven.Domain;
using Haven.Domain.Aggregates;
using NSubstitute;
using Shouldly;

namespace Haven.Application.Tests.Features.Environments.Commands.UpdateEnvironment;

[Category("Unit")]
public sealed class UpdateEnvironmentHandlerTests
{
    private IProjectRepository _projectRepository;
    private IUnitOfWork _unitOfWork;
    private UpdateEnvironmentHandler _sut;

    [SetUp]
    public void Setup()
    {
        _projectRepository = Substitute.For<IProjectRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _sut = new UpdateEnvironmentHandler(_projectRepository, _unitOfWork);
    }

    [Test]
    public async Task Handle_ShouldReturnFailure_WhenProjectDoesNotExist()
    {
        var command = CreateCommand(Guid.NewGuid(), Guid.NewGuid());
        _projectRepository.GetByIdWithEnvironmentsAsync(command.ProjectId, Arg.Any<CancellationToken>())
            .Returns((Project?)null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ShouldReturnFailure_WhenEnvironmentDoesNotExist()
    {
        var project = Project.Create("test-project");
        var command = CreateCommand(project.Id, Guid.NewGuid());

        _projectRepository.GetByIdWithEnvironmentsAsync(command.ProjectId, Arg.Any<CancellationToken>())
            .Returns(project);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ShouldReturnFailure_WhenNewNameConflictsWithAnotherEnvironment()
    {
        var project = Project.Create("test-project");
        project.AddEnvironment("production");
        var targetEnv = project.AddEnvironment("staging");

        var command = CreateCommand(project.Id, targetEnv.Id);
        command.Name = "production";

        _projectRepository.GetByIdWithEnvironmentsAsync(command.ProjectId, Arg.Any<CancellationToken>())
            .Returns(project);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ShouldReturnFailure_WhenNewNameConflicts_CaseInsensitive()
    {
        var project = Project.Create("test-project");
        project.AddEnvironment("production");
        var targetEnv = project.AddEnvironment("staging");

        var command = CreateCommand(project.Id, targetEnv.Id);
        command.Name = "PRODUCTION";

        _projectRepository.GetByIdWithEnvironmentsAsync(command.ProjectId, Arg.Any<CancellationToken>())
            .Returns(project);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_ShouldUpdateEnvironment_AndPersist()
    {
        var project = Project.Create("test-project");
        var environment = project.AddEnvironment("staging", "Old description");

        var command = CreateCommand(project.Id, environment.Id);
        command.Name = "production";
        command.Description = "New description";

        _projectRepository.GetByIdWithEnvironmentsAsync(command.ProjectId, Arg.Any<CancellationToken>())
            .Returns(project);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(environment.Id);

        environment.Name.ShouldBe("production");
        environment.Description.ShouldBe("New description");

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ShouldAllowUpdatingToSameName()
    {
        var project = Project.Create("test-project");
        var environment = project.AddEnvironment("staging");

        var command = CreateCommand(project.Id, environment.Id);
        command.Name = "staging";

        _projectRepository.GetByIdWithEnvironmentsAsync(command.ProjectId, Arg.Any<CancellationToken>())
            .Returns(project);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_ShouldUpdateOnlyDescription_WhenNameNotProvided()
    {
        var project = Project.Create("test-project");
        var environment = project.AddEnvironment("staging", "Old description");

        var command = CreateCommand(project.Id, environment.Id);
        command.Name = Optional<string>.None;
        command.Description = "Updated description";

        _projectRepository.GetByIdWithEnvironmentsAsync(command.ProjectId, Arg.Any<CancellationToken>())
            .Returns(project);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        environment.Name.ShouldBe("staging");
        environment.Description.ShouldBe("Updated description");
    }

    private static UpdateEnvironmentCommand CreateCommand(Guid projectId, Guid environmentId) => new()
    {
        ProjectId = projectId,
        EnvironmentId = environmentId,
        Name = "updated-env",
        Description = "Updated description"
    };
}
