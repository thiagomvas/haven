using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.Environments.Commands.CreateEnvironment;
using Haven.Domain.Aggregates;
using NSubstitute;
using Shouldly;


namespace Haven.Application.Tests.Features.Environments.Commands.CreateEnvironment;

[Category("Unit")]
public sealed class CreateEnvironmentHandlerTests
{
    private IProjectRepository _projectRepository;
    private IUnitOfWork _unitOfWork;
    private CreateEnvironmentHandler _sut;

    [SetUp]
    public void Setup()
    {
        _projectRepository = Substitute.For<IProjectRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _sut = new CreateEnvironmentHandler(_projectRepository, _unitOfWork);
    }

    [Test]
    public async Task Handle_ShouldReturnFailure_WhenProjectDoesNotExist()
    {
        var command = CreateCommand();
        _projectRepository.GetByIdWithEnvironmentsAsync(command.ProjectId, Arg.Any<CancellationToken>())
            .Returns((Project?)null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ShouldReturnFailure_WhenEnvironmentNameAlreadyExists()
    {
        var command = CreateCommand();
        var project = Project.Create("test-project");
        project.AddEnvironment(command.Name);

        _projectRepository.GetByIdWithEnvironmentsAsync(command.ProjectId, Arg.Any<CancellationToken>())
            .Returns(project);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ShouldReturnFailure_WhenEnvironmentNameAlreadyExists_CaseInsensitive()
    {
        var command = CreateCommand();
        command.Name = "Staging";
        var project = Project.Create("test-project");
        project.AddEnvironment("staging");

        _projectRepository.GetByIdWithEnvironmentsAsync(command.ProjectId, Arg.Any<CancellationToken>())
            .Returns(project);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_ShouldCreateEnvironment_AndPersist()
    {
        var command = CreateCommand();
        var project = Project.Create("test-project");

        _projectRepository.GetByIdWithEnvironmentsAsync(command.ProjectId, Arg.Any<CancellationToken>())
            .Returns(project);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBe(Guid.Empty);

        var environment = project.Environments.FirstOrDefault(e => e.Id == result.Value);
        environment.ShouldNotBeNull();
        environment.Name.ShouldBe(command.Name);
        environment.Description.ShouldBe(command.Description);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ShouldReturnEnvironmentId_OnSuccess()
    {
        var command = CreateCommand();
        var project = Project.Create("test-project");

        _projectRepository.GetByIdWithEnvironmentsAsync(command.ProjectId, Arg.Any<CancellationToken>())
            .Returns(project);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBe(Guid.Empty);
        project.Environments.Any(e => e.Id == result.Value).ShouldBeTrue();
    }

    private static CreateEnvironmentCommand CreateCommand() => new()
    {
        ProjectId = Guid.NewGuid(),
        Name = "staging",
        Description = "Staging environment"
    };
}
