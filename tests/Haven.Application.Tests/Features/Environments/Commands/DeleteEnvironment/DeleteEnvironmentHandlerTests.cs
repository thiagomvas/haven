using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.Environments.Commands.DeleteEnvironment;
using Haven.Domain.Aggregates;
using NSubstitute;
using Shouldly;


namespace Haven.Application.Tests.Features.Environments.Commands.DeleteEnvironment;

[Category("Unit")]
public sealed class DeleteEnvironmentHandlerTests
{
    private IProjectRepository _projectRepository;
    private DeleteEnvironmentHandler _sut;

    [SetUp]
    public void Setup()
    {
        _projectRepository = Substitute.For<IProjectRepository>();
        _sut = new DeleteEnvironmentHandler(_projectRepository);
    }

    [Test]
    public async Task Handle_ShouldReturnFailure_WhenProjectDoesNotExist()
    {
        var command = CreateCommand(Guid.NewGuid(), Guid.NewGuid());
        _projectRepository.GetByIdAsync(command.ProjectId, Arg.Any<CancellationToken>())
            .Returns((Project?)null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        
    }

    [Test]
    public async Task Handle_ShouldReturnFailure_WhenEnvironmentDoesNotExist()
    {
        var project = Project.Create("test-project");
        var command = CreateCommand(project.Id, Guid.NewGuid());

        _projectRepository.GetByIdAsync(command.ProjectId, Arg.Any<CancellationToken>())
            .Returns(project);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        
    }

    [Test]
    public async Task Handle_ShouldDeleteEnvironment_AndPersist()
    {
        var project = Project.Create("test-project");
        var environment = project.AddEnvironment("staging");
        var command = CreateCommand(project.Id, environment.Id);

        _projectRepository.GetByIdAsync(command.ProjectId, Arg.Any<CancellationToken>())
            .Returns(project);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        project.Environments.Any(e => e.Id == environment.Id).ShouldBeFalse();
        
    }

    [Test]
    public async Task Handle_ShouldNotAffectOtherEnvironments_WhenDeletingOne()
    {
        var project = Project.Create("test-project");
        var staging = project.AddEnvironment("staging");
        var production = project.AddEnvironment("production");
        var command = CreateCommand(project.Id, staging.Id);

        _projectRepository.GetByIdAsync(command.ProjectId, Arg.Any<CancellationToken>())
            .Returns(project);

        await _sut.Handle(command, CancellationToken.None);

        project.Environments.Any(e => e.Id == staging.Id).ShouldBeFalse();
        project.Environments.Any(e => e.Id == production.Id).ShouldBeTrue();
    }

    private static DeleteEnvironmentCommand CreateCommand(Guid projectId, Guid environmentId) => new()
    {
        ProjectId = projectId,
        EnvironmentId = environmentId
    };
}
