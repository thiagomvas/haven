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
public class WriteEnvironmentOnEnvironmentUpdatedEventHandlerTests
{
    private HavenDbContext _context = null!;
    private IProjectRepository _projectRepository = null!;
    private IEnvironmentRepository _environmentRepository = null!;
    private IManifestSerializer<Environment> _mockSerializer = null!;
    private WriteEnvironmentOnEnvironmentUpdatedEventHandler _handler = null!;

    [SetUp]
    public async Task SetUp()
    {
        _context = CreateDbContext();
        await _context.Database.EnsureCreatedAsync();

        _projectRepository = new ProjectRepository(_context);
        _environmentRepository = new EnvironmentRepository(_context);
        _mockSerializer = Substitute.For<IManifestSerializer<Environment>>();
        _handler = new WriteEnvironmentOnEnvironmentUpdatedEventHandler(_mockSerializer, _environmentRepository);
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
    public async Task Handle_WithNameChange_CallsRenameAsyncThenWriteAsync()
    {
        // Arrange
        var project = Project.Create("Test Project");
        await _projectRepository.AddAsync(project, CancellationToken.None);
        var environment = project.AddEnvironment("Old Name");
        await _context.SaveChangesAsync();

        var @event = new EnvironmentUpdatedEvent(environment.Id, "Old Name", "New Name");

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        Received.InOrder(async () =>
        {
            await _mockSerializer.RenameAsync(Arg.Any<Environment>(), "Old Name", "New Name", Arg.Any<CancellationToken>());
            await _mockSerializer.WriteAsync(Arg.Any<Environment>(), Arg.Any<CancellationToken>());
        });
    }

    [Test]
    public async Task Handle_WithoutNameChange_SkipsRenameAndCallsWriteAsync()
    {
        // Arrange
        var project = Project.Create("Test Project");
        await _projectRepository.AddAsync(project, CancellationToken.None);
        var environment = project.AddEnvironment("Same Name");
        await _context.SaveChangesAsync();

        var @event = new EnvironmentUpdatedEvent(environment.Id, "Same Name", "Same Name");

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        await _mockSerializer.DidNotReceive().RenameAsync(Arg.Any<Environment>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _mockSerializer.Received(1).WriteAsync(Arg.Any<Environment>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithNonexistentEnvironment_DoesNotCallAnyMethod()
    {
        // Arrange
        var @event = new EnvironmentUpdatedEvent(Guid.NewGuid(), "Old Name", "New Name");

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        await _mockSerializer.DidNotReceive().RenameAsync(Arg.Any<Environment>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _mockSerializer.DidNotReceive().WriteAsync(Arg.Any<Environment>(), Arg.Any<CancellationToken>());
    }
}