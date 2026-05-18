using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.Manifests.EventHandlers;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Events;
using Haven.Domain.ValueObjects;
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
public class WriteServiceOnManifestDirtyEventHandlerTests
{
    private HavenDbContext _context = null!;
    private IServiceRepository _serviceRepository = null!;
    private IManifestSerializer<Service> _mockSerializer = null!;
    private WriteServiceOnManifestDirtyEventHandler _handler = null!;

    [SetUp]
    public async Task SetUp()
    {
        _context = CreateDbContext();
        await _context.Database.EnsureCreatedAsync();

        _serviceRepository = new ServiceRepository(_context);
        _mockSerializer = Substitute.For<IManifestSerializer<Service>>();
        _handler = new WriteServiceOnManifestDirtyEventHandler(
            _mockSerializer,
            _serviceRepository);
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
    public async Task Handle_WithServiceEntityType_SerializesOnlyThatService()
    {
        // Arrange
        var project = Project.Create("Test Project");
        await _context.Projects.AddAsync(project, CancellationToken.None);
        await _context.SaveChangesAsync();

        var environment = Environment.Create(project.Id, "Dev");
        await _context.Environments.AddAsync(environment, CancellationToken.None);
        await _context.SaveChangesAsync();

        var service1 = Service.Create(environment.Id, "Service 1", ServiceType.Process, ExposureMode.Internal);
        var service2 = Service.Create(environment.Id, "Service 2", ServiceType.Process, ExposureMode.Internal);

        await _serviceRepository.AddAsync(service1, CancellationToken.None);
        await _serviceRepository.AddAsync(service2, CancellationToken.None);
        await _context.SaveChangesAsync();

        var @event = new ManifestDirtyEvent(EntityType.Service, service1.Id);

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        await _mockSerializer.Received(1).WriteAsync(
            Arg.Is<Service>(s => s.Name == "Service 1"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithEnvironmentEntityType_IgnoresEvent()
    {
        // Arrange
        var project = Project.Create("Test Project");
        await _context.Projects.AddAsync(project, CancellationToken.None);
        await _context.SaveChangesAsync();

        var environment = Environment.Create(project.Id, "Dev");
        await _context.Environments.AddAsync(environment, CancellationToken.None);
        await _context.SaveChangesAsync();

        var service = Service.Create(environment.Id, "Service 1", ServiceType.Process, ExposureMode.Internal);
        await _serviceRepository.AddAsync(service, CancellationToken.None);
        await _context.SaveChangesAsync();

        var @event = new ManifestDirtyEvent(EntityType.Environment, environment.Id);

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        await _mockSerializer.DidNotReceive().WriteAsync(Arg.Any<Service>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithNonexistentServiceId_DoesNotSerialize()
    {
        // Arrange
        var @event = new ManifestDirtyEvent(EntityType.Service, Guid.NewGuid());

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        await _mockSerializer.DidNotReceive().WriteAsync(Arg.Any<Service>(), Arg.Any<CancellationToken>());
    }
}
