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
public class WriteProjectOnProjectUpdatedEventHandlerTests
{
    private HavenDbContext _context = null!;
    private IProjectRepository _projectRepository = null!;
    private IManifestSerializer<Project> _mockSerializer = null!;
    private WriteProjectOnProjectUpdatedEventHandler _handler = null!;

    [SetUp]
    public async Task SetUp()
    {
        _context = CreateDbContext();
        await _context.Database.EnsureCreatedAsync();

        _projectRepository = new ProjectRepository(_context);
        _mockSerializer = Substitute.For<IManifestSerializer<Project>>();
        _handler = new WriteProjectOnProjectUpdatedEventHandler(_mockSerializer, _projectRepository);
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
        var project = Project.Create("Old Name");
        await _projectRepository.AddAsync(project, CancellationToken.None);
        await _context.SaveChangesAsync();

        var @event = new ProjectUpdatedEvent(project.Id, "Old Name", "New Name");

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        Received.InOrder(async () =>
        {
            await _mockSerializer.RenameAsync(Arg.Any<Project>(), "Old Name", "New Name", Arg.Any<CancellationToken>());
            await _mockSerializer.WriteAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>());
        });
    }

    [Test]
    public async Task Handle_WithoutNameChange_SkipsRenameAndCallsWriteAsync()
    {
        // Arrange
        var project = Project.Create("Same Name");
        await _projectRepository.AddAsync(project, CancellationToken.None);
        await _context.SaveChangesAsync();

        var @event = new ProjectUpdatedEvent(project.Id, "Same Name", "Same Name");

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        await _mockSerializer.DidNotReceive().RenameAsync(Arg.Any<Project>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _mockSerializer.Received(1).WriteAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithNonexistentProject_DoesNotCallAnyMethod()
    {
        // Arrange
        var @event = new ProjectUpdatedEvent(Guid.NewGuid(), "Old Name", "New Name");

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        await _mockSerializer.DidNotReceive().RenameAsync(Arg.Any<Project>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _mockSerializer.DidNotReceive().WriteAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>());
    }
}