using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.Manifests.EventHandlers;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Events;
using Haven.Domain.Models;
using Haven.Domain.ValueObjects;
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
public class WriteServiceOnServiceDeletedEventHandlerTests
{
    private HavenDbContext _context = null!;
    private IProjectRepository _projectRepository = null!;
    private IServiceRepository _serviceRepository = null!;
    private IManifestSerializer<Service> _mockSerializer = null!;
    private WriteServiceOnServiceDeletedEventHandler _handler = null!;

    [SetUp]
    public async Task SetUp()
    {
        _context = CreateDbContext();
        await _context.Database.EnsureCreatedAsync();

        _projectRepository = new ProjectRepository(_context);
        _serviceRepository = new ServiceRepository(_context);
        _mockSerializer = Substitute.For<IManifestSerializer<Service>>();
        _handler = new WriteServiceOnServiceDeletedEventHandler(_mockSerializer, _serviceRepository);
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
    public async Task Handle_WithExistingService_CallsRemoveAsync()
    {
        // Arrange
        var project = Project.Create("Test Project");
        await _projectRepository.AddAsync(project, CancellationToken.None);
        var environment = project.AddEnvironment("Test Environment");
        var service = environment.AddService("test-service", ServiceType.Process, ExposureMode.Internal);
        await _context.SaveChangesAsync();

        var @event = new ServiceDeletedEvent(service.Id, service.Name);

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        await _mockSerializer.Received(1).RemoveAsync(Arg.Any<Service>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithNonexistentService_DoesNotCallRemoveAsync()
    {
        // Arrange
        var @event = new ServiceDeletedEvent(Guid.NewGuid(), "test-service");

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        await _mockSerializer.DidNotReceive().RemoveAsync(Arg.Any<Service>(), Arg.Any<CancellationToken>());
    }
}
