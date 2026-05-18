using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.Manifests.EventHandlers;
using Haven.Domain.Aggregates;
using Haven.Domain.Events;
using Haven.Infrastructure.Persistence;
using Haven.Infrastructure.Persistence.Interceptors;
using Haven.Infrastructure.Persistence.Repositories;
using Mediator;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;

namespace Haven.Integration.Tests.Features.Manifests;

[TestFixture]
[Category("Integration")]
public class WriteProjectOnManifestDirtyEventHandlerTests
{
    private HavenDbContext _context = null!;
    private IProjectRepository _projectRepository = null!;
    private IManifestSerializer<Project> _mockSerializer = null!;
    private WriteProjectOnManifestDirtyEventHandler _handler = null!;

    [SetUp]
    public async Task SetUp()
    {
        _context = CreateDbContext();
        await _context.Database.EnsureCreatedAsync();

        _projectRepository = new ProjectRepository(_context);
        _mockSerializer = Substitute.For<IManifestSerializer<Project>>();
        _handler = new WriteProjectOnManifestDirtyEventHandler(_mockSerializer, _projectRepository);
    }

    [TearDown]
    public void TearDown()
    {
        _context?.Dispose();
    }

    private static HavenDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<HavenDbContext>()
            .UseSqlite($"DataSource=file:memdb{Guid.NewGuid()}?mode=memory&cache=shared")
            .Options;

        var mediator = Substitute.For<IMediator>();
        var domainEventInterceptor = new DomainEventInterceptor(mediator);
        var softDeleteInterceptor = new SoftDeleteInterceptor();
        var encryptionService = Substitute.For<IEncryptionService>();

        return new HavenDbContext(options, domainEventInterceptor, softDeleteInterceptor, encryptionService);
    }

    [Test]
    public async Task Handle_WithMultipleProjects_SerializesAllProjects()
    {
        // Arrange
        var project1 = Project.Create("Project 1");
        var project2 = Project.Create("Project 2");
        var project3 = Project.Create("Project 3");

        await _projectRepository.AddAsync(project1, CancellationToken.None);
        await _projectRepository.AddAsync(project2, CancellationToken.None);
        await _projectRepository.AddAsync(project3, CancellationToken.None);
        await _context.SaveChangesAsync();

        var @event = new ManifestDirtyEvent();

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        await _mockSerializer.Received(3).WriteAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>());
        await _mockSerializer.Received(1).WriteAsync(Arg.Is<Project>(p => p.Name == "Project 1"), Arg.Any<CancellationToken>());
        await _mockSerializer.Received(1).WriteAsync(Arg.Is<Project>(p => p.Name == "Project 2"), Arg.Any<CancellationToken>());
        await _mockSerializer.Received(1).WriteAsync(Arg.Is<Project>(p => p.Name == "Project 3"), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithNoProjects_DoesNotSerialize()
    {
        // Arrange
        var @event = new ManifestDirtyEvent();

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        await _mockSerializer.DidNotReceive().WriteAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithSingleProject_SerializesProject()
    {
        // Arrange
        var project = Project.Create("Test Project");
        await _projectRepository.AddAsync(project, CancellationToken.None);
        await _context.SaveChangesAsync();

        var @event = new ManifestDirtyEvent();

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        await _mockSerializer.Received(1).WriteAsync(
            Arg.Is<Project>(p => p.Name == "Test Project"),
            Arg.Any<CancellationToken>());
    }
}
