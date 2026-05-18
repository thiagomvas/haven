using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.Manifests.EventHandlers;
using Haven.Domain;
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
    private IEnvironmentRepository _environmentRepository = null!;
    private IManifestSerializer<Environment> _mockSerializer = null!;
    private WriteEnvironmentOnManifestDirtyEventHandler _handler = null!;

    [SetUp]
    public async Task SetUp()
    {
        _context = CreateDbContext();
        await _context.Database.EnsureCreatedAsync();

        _environmentRepository = new EnvironmentRepository(_context);
        _mockSerializer = Substitute.For<IManifestSerializer<Environment>>();
        _handler = new WriteEnvironmentOnManifestDirtyEventHandler(_mockSerializer, _environmentRepository);
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
    public async Task Handle_WithEnvironmentEntityType_SerializesOnlyThatEnvironment()
    {
        // Arrange
        var project = Project.Create("Test Project");
        await _context.Projects.AddAsync(project, CancellationToken.None);
        await _context.SaveChangesAsync();

        var dev = Environment.Create(project.Id, "Dev");
        var prod = Environment.Create(project.Id, "Prod");

        await _environmentRepository.AddAsync(dev, CancellationToken.None);
        await _environmentRepository.AddAsync(prod, CancellationToken.None);
        await _context.SaveChangesAsync();

        var @event = new ManifestDirtyEvent(EntityType.Environment, prod.Id);

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        await _mockSerializer.Received(1).WriteAsync(
            Arg.Is<Environment>(e => e.Name == "Prod"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithProjectEntityType_IgnoresEvent()
    {
        // Arrange
        var project = Project.Create("Test Project");
        await _context.Projects.AddAsync(project, CancellationToken.None);
        await _context.SaveChangesAsync();

        var env = Environment.Create(project.Id, "Dev");
        await _environmentRepository.AddAsync(env, CancellationToken.None);
        await _context.SaveChangesAsync();

        var @event = new ManifestDirtyEvent(EntityType.Project, project.Id);

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        await _mockSerializer.DidNotReceive().WriteAsync(Arg.Any<Environment>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithNonexistentEnvironmentId_DoesNotSerialize()
    {
        // Arrange
        var @event = new ManifestDirtyEvent(EntityType.Environment, Guid.NewGuid());

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        await _mockSerializer.DidNotReceive().WriteAsync(Arg.Any<Environment>(), Arg.Any<CancellationToken>());
    }
}
