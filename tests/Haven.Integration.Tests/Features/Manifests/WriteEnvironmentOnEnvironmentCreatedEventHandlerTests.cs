using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.Manifests.EventHandlers;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
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
public class WriteEnvironmentOnEnvironmentCreatedEventHandlerTests
{
    private HavenDbContext _context = null!;
    private IProjectRepository _projectRepository = null!;
    private IEnvironmentRepository _environmentRepository = null!;
    private IManifestSerializer<Environment> _mockSerializer = null!;
    private WriteEnvironmentOnEnvironmentCreatedEventHandler _handler = null!;

    [SetUp]
    public async Task SetUp()
    {
        _context = CreateDbContext();
        await _context.Database.EnsureCreatedAsync();

        _projectRepository = new ProjectRepository(_context);
        _environmentRepository = new EnvironmentRepository(_context);
        _mockSerializer = Substitute.For<IManifestSerializer<Environment>>();
        _handler = new WriteEnvironmentOnEnvironmentCreatedEventHandler(_mockSerializer, _environmentRepository);
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
    public async Task Handle_WithExistingEnvironment_CallsWriteAsync()
    {
        // Arrange
        var project = Project.Create("Test Project");
        await _projectRepository.AddAsync(project, CancellationToken.None);
        var environment = project.AddEnvironment("Test Environment");
        await _context.SaveChangesAsync();

        var @event = new EnvironmentCreatedEvent(environment.Id, environment.Name);

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        await _mockSerializer.Received(1).WriteAsync(Arg.Is<Environment>(e => e.Name == "Test Environment"), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithNonexistentEnvironment_DoesNotCallWriteAsync()
    {
        // Arrange
        var @event = new EnvironmentCreatedEvent(Guid.NewGuid(), "Test Environment");

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        await _mockSerializer.DidNotReceive().WriteAsync(Arg.Any<Environment>(), Arg.Any<CancellationToken>());
    }
}