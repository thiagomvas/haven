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
using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Integration.Tests.Features.Manifests;

[TestFixture]
[Category("Integration")]
public class WriteEnvironmentOnManifestDirtyEventHandlerTests
{
    private HavenDbContext _context = null!;
    private IProjectRepository _projectRepository = null!;
    private IEnvironmentRepository _environmentRepository = null!;
    private IManifestSerializer _mockSerializer = null!;
    private WriteEnvironmentOnManifestDirtyEventHandler _handler = null!;

    [SetUp]
    public async Task SetUp()
    {
        _context = CreateDbContext();
        await _context.Database.EnsureCreatedAsync();

        _projectRepository = new ProjectRepository(_context);
        _environmentRepository = new EnvironmentRepository(_context);
        _mockSerializer = Substitute.For<IManifestSerializer>();
        _handler = new WriteEnvironmentOnManifestDirtyEventHandler(_mockSerializer, _projectRepository, _environmentRepository);
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
    public async Task Handle_WithEnvironments_SerializesAllEnvironments()
    {
        // Arrange
        var project = Project.Create("Test Project");
        await _projectRepository.AddAsync(project, CancellationToken.None);
        await _context.SaveChangesAsync();

        var env1 = Environment.Create(project.Id, "Dev");
        var env2 = Environment.Create(project.Id, "Prod");

        await _environmentRepository.AddAsync(env1, CancellationToken.None);
        await _environmentRepository.AddAsync(env2, CancellationToken.None);
        await _context.SaveChangesAsync();

        var @event = new ManifestDirtyEvent();

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        await _mockSerializer.Received(2).WriteEnvironmentAsync(Arg.Any<Project>(), Arg.Any<Environment>(), Arg.Any<CancellationToken>());
        await _mockSerializer.Received(1).WriteEnvironmentAsync(
            Arg.Is<Project>(p => p.Name == "Test Project"),
            Arg.Is<Environment>(e => e.Name == "Dev"),
            Arg.Any<CancellationToken>());
        await _mockSerializer.Received(1).WriteEnvironmentAsync(
            Arg.Is<Project>(p => p.Name == "Test Project"),
            Arg.Is<Environment>(e => e.Name == "Prod"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithMultipleProjectsAndEnvironments_SerializesAllEnvironments()
    {
        // Arrange
        var project1 = Project.Create("Project 1");
        var project2 = Project.Create("Project 2");

        await _projectRepository.AddAsync(project1, CancellationToken.None);
        await _projectRepository.AddAsync(project2, CancellationToken.None);
        await _context.SaveChangesAsync();

        var env1 = Environment.Create(project1.Id, "Dev");
        var env2 = Environment.Create(project2.Id, "Prod");

        await _environmentRepository.AddAsync(env1, CancellationToken.None);
        await _environmentRepository.AddAsync(env2, CancellationToken.None);
        await _context.SaveChangesAsync();

        var @event = new ManifestDirtyEvent();

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        await _mockSerializer.Received(2).WriteEnvironmentAsync(Arg.Any<Project>(), Arg.Any<Environment>(), Arg.Any<CancellationToken>());
        await _mockSerializer.Received(1).WriteEnvironmentAsync(
            Arg.Is<Project>(p => p.Name == "Project 1"),
            Arg.Is<Environment>(e => e.Name == "Dev"),
            Arg.Any<CancellationToken>());
        await _mockSerializer.Received(1).WriteEnvironmentAsync(
            Arg.Is<Project>(p => p.Name == "Project 2"),
            Arg.Is<Environment>(e => e.Name == "Prod"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithNoEnvironments_DoesNotSerialize()
    {
        // Arrange
        var project = Project.Create("Test Project");
        await _projectRepository.AddAsync(project, CancellationToken.None);
        await _context.SaveChangesAsync();

        var @event = new ManifestDirtyEvent();

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        await _mockSerializer.DidNotReceive().WriteEnvironmentAsync(
            Arg.Any<Project>(),
            Arg.Any<Environment>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithMultipleEnvironmentsInSingleProject_SerializesAll()
    {
        // Arrange
        var project = Project.Create("Test Project");
        await _projectRepository.AddAsync(project, CancellationToken.None);
        await _context.SaveChangesAsync();

        var dev = Environment.Create(project.Id, "Dev");
        var staging = Environment.Create(project.Id, "Staging");
        var prod = Environment.Create(project.Id, "Prod");

        await _environmentRepository.AddAsync(dev, CancellationToken.None);
        await _environmentRepository.AddAsync(staging, CancellationToken.None);
        await _environmentRepository.AddAsync(prod, CancellationToken.None);
        await _context.SaveChangesAsync();

        var @event = new ManifestDirtyEvent();

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        await _mockSerializer.Received(3).WriteEnvironmentAsync(Arg.Any<Project>(), Arg.Any<Environment>(), Arg.Any<CancellationToken>());
    }
}
