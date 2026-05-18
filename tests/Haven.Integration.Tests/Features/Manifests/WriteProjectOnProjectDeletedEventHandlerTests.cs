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
public class WriteProjectOnProjectDeletedEventHandlerTests
{
    private HavenDbContext _context = null!;
    private IProjectRepository _projectRepository = null!;
    private IManifestSerializer<Project> _mockSerializer = null!;
    private WriteProjectOnProjectDeletedEventHandler _handler = null!;

    [SetUp]
    public async Task SetUp()
    {
        _context = CreateDbContext();
        await _context.Database.EnsureCreatedAsync();

        _projectRepository = new ProjectRepository(_context);
        _mockSerializer = Substitute.For<IManifestSerializer<Project>>();
        _handler = new WriteProjectOnProjectDeletedEventHandler(_mockSerializer, _projectRepository);
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
    public async Task Handle_WithExistingProject_CallsRemoveAsync()
    {
        // Arrange
        var project = Project.Create("Test Project");
        await _projectRepository.AddAsync(project, CancellationToken.None);
        await _context.SaveChangesAsync();

        var @event = new ProjectDeletedEvent(project.Id, project.Name);

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        await _mockSerializer.Received(1).RemoveAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithNonexistentProject_DoesNotCallRemoveAsync()
    {
        // Arrange
        var @event = new ProjectDeletedEvent(Guid.NewGuid(), "Test Project");

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        await _mockSerializer.DidNotReceive().RemoveAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>());
    }
}
