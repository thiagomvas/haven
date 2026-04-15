using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.Projects.Commands.CreateProject;
using Haven.Domain.Aggregates;
using NSubstitute;
using Shouldly;

namespace Haven.Application.Tests.Features.Projects.Commands.CreateProject;

[Category("Unit")]
public sealed class CreateProjectHandlerTests
{
    private IProjectRepository _projectRepository;
    private IUnitOfWork _unitOfWork;
    private CreateProjectHandler _sut;

    [SetUp]
    public void Setup()
    {
        _projectRepository = Substitute.For<IProjectRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _sut = new CreateProjectHandler(_projectRepository, _unitOfWork);
    }

    [Test]
    public async Task Handle_ShouldCreateProject_AndPersist()
    {
        var command = CreateCommand();
        var projectId = Guid.NewGuid();

        _projectRepository.AddAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>())
            .Returns(projectId);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(projectId);

        await _projectRepository.Received(1)
            .AddAsync(Arg.Is<Project>(p =>
                p.Name == command.Name &&
                p.Description == command.Description),
                Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ShouldReturnSuccessResult_WithProjectId()
    {
        var command = CreateCommand();
        var projectId = Guid.NewGuid();

        _projectRepository.AddAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>())
            .Returns(projectId);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(projectId);
    }

    public static CreateProjectCommand CreateCommand()
    {
        return new()
        {
            Name = "Project Name",
            Description = "Project Description"
        };
    }
}