using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.Environments.Queries;
using Haven.Application.Features.Environments.Queries.GetEnvironmentsByProject;
using Haven.Domain.Aggregates;
using NSubstitute;
using Shouldly;
using Environment = Haven.Domain.Entities.Environment;


namespace Haven.Application.Tests.Features.Environments.Queries.GetEnvironmentsByProject;

[Category("Unit")]
public sealed class GetEnvironmentsByProjectHandlerTests
{
    private IProjectRepository _projectRepository;
    private IEnvironmentRepository _environmentRepository;
    private GetEnvironmentsByProjectHandler _sut;

    [SetUp]
    public void Setup()
    {
        _projectRepository = Substitute.For<IProjectRepository>();
        _environmentRepository = Substitute.For<IEnvironmentRepository>();
        _sut = new GetEnvironmentsByProjectHandler(_projectRepository, _environmentRepository);
    }

    [Test]
    public async Task Handle_ShouldReturnFailure_WhenProjectDoesNotExist()
    {
        var query = CreateQuery(Guid.NewGuid());
        _projectRepository.GetByIdAsync(query.ProjectId, Arg.Any<CancellationToken>())
            .Returns((Project?)null);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        await _environmentRepository.DidNotReceive()
            .GetByProjectIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ShouldReturnEmptyList_WhenProjectHasNoEnvironments()
    {
        var project = Project.Create("test-project");
        var query = CreateQuery(project.Id);

        _projectRepository.GetByIdAsync(query.ProjectId, Arg.Any<CancellationToken>())
            .Returns(project);
        _environmentRepository.GetByProjectIdAsync(query.ProjectId, Arg.Any<CancellationToken>())
            .Returns(new List<Environment>().AsReadOnly() as IReadOnlyList<Environment>);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    [Test]
    public async Task Handle_ShouldReturnEnvironmentDtos_MappedCorrectly()
    {
        var projectId = Guid.NewGuid();
        var project = Project.Create("test-project");
        var query = CreateQuery(projectId);

        var environments = new List<Environment>
        {
            BuildEnvironment(projectId, "staging", "Staging env"),
            BuildEnvironment(projectId, "production", null)
        };

        _projectRepository.GetByIdAsync(query.ProjectId, Arg.Any<CancellationToken>())
            .Returns(project);
        _environmentRepository.GetByProjectIdAsync(query.ProjectId, Arg.Any<CancellationToken>())
            .Returns(environments as IReadOnlyList<Environment>);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(2);

        var stagingDto = result.Value.FirstOrDefault(e => e.Name == "staging");
        stagingDto.ShouldNotBeNull();
        stagingDto.ProjectId.ShouldBe(projectId);
        stagingDto.Description.ShouldBe("Staging env");
        stagingDto.NetworkName.ShouldNotBeNullOrEmpty();

        var productionDto = result.Value.FirstOrDefault(e => e.Name == "production");
        productionDto.ShouldNotBeNull();
        productionDto.Description.ShouldBeNull();
    }

    [Test]
    public async Task Handle_ShouldQueryEnvironmentRepository_WithCorrectProjectId()
    {
        var project = Project.Create("test-project");
        var query = CreateQuery(project.Id);

        _projectRepository.GetByIdAsync(query.ProjectId, Arg.Any<CancellationToken>())
            .Returns(project);
        _environmentRepository.GetByProjectIdAsync(query.ProjectId, Arg.Any<CancellationToken>())
            .Returns(new List<Environment>() as IReadOnlyList<Environment>);

        await _sut.Handle(query, CancellationToken.None);

        await _environmentRepository.Received(1)
            .GetByProjectIdAsync(query.ProjectId, Arg.Any<CancellationToken>());
    }

    private static GetEnvironmentsByProjectQuery CreateQuery(Guid projectId) => new()
    {
        ProjectId = projectId
    };

    private static Environment BuildEnvironment(Guid projectId, string name, string? description)
    {
        var project = Project.Reconstitute(projectId, "test-project", null, null);
        return project.AddEnvironment(name, description: description);
    }
}
