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
    private IProjectRepository _projectRepository = null!;
    private IEnvironmentRepository _environmentRepository = null!;
    private IServiceRepository _serviceRepository = null!;
    private IManifestSerializer _mockSerializer = null!;
    private WriteServiceOnManifestDirtyEventHandler _handler = null!;

    [SetUp]
    public async Task SetUp()
    {
        _context = CreateDbContext();
        await _context.Database.EnsureCreatedAsync();

        _projectRepository = new ProjectRepository(_context);
        _environmentRepository = new EnvironmentRepository(_context);
        _serviceRepository = new ServiceRepository(_context);
        _mockSerializer = Substitute.For<IManifestSerializer>();
        _handler = new WriteServiceOnManifestDirtyEventHandler(
            _mockSerializer,
            _projectRepository,
            _environmentRepository,
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
    public async Task Handle_WithServices_SerializesAllServices()
    {
        // Arrange
        var project = Project.Create("Test Project");
        await _projectRepository.AddAsync(project, CancellationToken.None);
        await _context.SaveChangesAsync();

        var environment = Environment.Create(project.Id, "Dev");
        await _environmentRepository.AddAsync(environment, CancellationToken.None);
        await _context.SaveChangesAsync();

        var service1 = Service.Create(environment.Id, "Service 1", ServiceType.Process, ExposureMode.Internal);
        var service2 = Service.Create(environment.Id, "Service 2", ServiceType.Process, ExposureMode.Internal);

        await _serviceRepository.AddAsync(service1, CancellationToken.None);
        await _serviceRepository.AddAsync(service2, CancellationToken.None);
        await _context.SaveChangesAsync();

        var @event = new ManifestDirtyEvent();

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        await _mockSerializer.Received(2).WriteServiceAsync(
            Arg.Any<Project>(),
            Arg.Any<Environment>(),
            Arg.Any<Service>(),
            Arg.Any<CancellationToken>());
        await _mockSerializer.Received(1).WriteServiceAsync(
            Arg.Is<Project>(p => p.Name == "Test Project"),
            Arg.Is<Environment>(e => e.Name == "Dev"),
            Arg.Is<Service>(s => s.Name == "Service 1"),
            Arg.Any<CancellationToken>());
        await _mockSerializer.Received(1).WriteServiceAsync(
            Arg.Is<Project>(p => p.Name == "Test Project"),
            Arg.Is<Environment>(e => e.Name == "Dev"),
            Arg.Is<Service>(s => s.Name == "Service 2"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithMultipleEnvironments_SerializesAllServices()
    {
        // Arrange
        var project = Project.Create("Test Project");
        await _projectRepository.AddAsync(project, CancellationToken.None);
        await _context.SaveChangesAsync();

        var devEnv = Environment.Create(project.Id, "Dev");
        var prodEnv = Environment.Create(project.Id, "Prod");

        await _environmentRepository.AddAsync(devEnv, CancellationToken.None);
        await _environmentRepository.AddAsync(prodEnv, CancellationToken.None);
        await _context.SaveChangesAsync();

        var devService = Service.Create(devEnv.Id, "Dev Service", ServiceType.Process, ExposureMode.Internal);
        var prodService = Service.Create(prodEnv.Id, "Prod Service", ServiceType.Process, ExposureMode.Internal);

        await _serviceRepository.AddAsync(devService, CancellationToken.None);
        await _serviceRepository.AddAsync(prodService, CancellationToken.None);
        await _context.SaveChangesAsync();

        var @event = new ManifestDirtyEvent();

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        await _mockSerializer.Received(2).WriteServiceAsync(
            Arg.Any<Project>(),
            Arg.Any<Environment>(),
            Arg.Any<Service>(),
            Arg.Any<CancellationToken>());
        await _mockSerializer.Received(1).WriteServiceAsync(
            Arg.Is<Project>(p => p.Name == "Test Project"),
            Arg.Is<Environment>(e => e.Name == "Dev"),
            Arg.Is<Service>(s => s.Name == "Dev Service"),
            Arg.Any<CancellationToken>());
        await _mockSerializer.Received(1).WriteServiceAsync(
            Arg.Is<Project>(p => p.Name == "Test Project"),
            Arg.Is<Environment>(e => e.Name == "Prod"),
            Arg.Is<Service>(s => s.Name == "Prod Service"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithMultipleProjects_SerializesAllServices()
    {
        // Arrange
        var project1 = Project.Create("Project 1");
        var project2 = Project.Create("Project 2");

        await _projectRepository.AddAsync(project1, CancellationToken.None);
        await _projectRepository.AddAsync(project2, CancellationToken.None);
        await _context.SaveChangesAsync();

        var env1 = Environment.Create(project1.Id, "Dev");
        var env2 = Environment.Create(project2.Id, "Dev");

        await _environmentRepository.AddAsync(env1, CancellationToken.None);
        await _environmentRepository.AddAsync(env2, CancellationToken.None);
        await _context.SaveChangesAsync();

        var service1 = Service.Create(env1.Id, "Service 1", ServiceType.Process, ExposureMode.Internal);
        var service2 = Service.Create(env2.Id, "Service 2", ServiceType.Process, ExposureMode.Internal);

        await _serviceRepository.AddAsync(service1, CancellationToken.None);
        await _serviceRepository.AddAsync(service2, CancellationToken.None);
        await _context.SaveChangesAsync();

        var @event = new ManifestDirtyEvent();

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        await _mockSerializer.Received(2).WriteServiceAsync(
            Arg.Any<Project>(),
            Arg.Any<Environment>(),
            Arg.Any<Service>(),
            Arg.Any<CancellationToken>());
        await _mockSerializer.Received(1).WriteServiceAsync(
            Arg.Is<Project>(p => p.Name == "Project 1"),
            Arg.Is<Environment>(e => e.Name == "Dev"),
            Arg.Is<Service>(s => s.Name == "Service 1"),
            Arg.Any<CancellationToken>());
        await _mockSerializer.Received(1).WriteServiceAsync(
            Arg.Is<Project>(p => p.Name == "Project 2"),
            Arg.Is<Environment>(e => e.Name == "Dev"),
            Arg.Is<Service>(s => s.Name == "Service 2"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithNoServices_DoesNotSerialize()
    {
        // Arrange
        var project = Project.Create("Test Project");
        await _projectRepository.AddAsync(project, CancellationToken.None);
        await _context.SaveChangesAsync();

        var environment = Environment.Create(project.Id, "Dev");
        await _environmentRepository.AddAsync(environment, CancellationToken.None);
        await _context.SaveChangesAsync();

        var @event = new ManifestDirtyEvent();

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        await _mockSerializer.DidNotReceive().WriteServiceAsync(
            Arg.Any<Project>(),
            Arg.Any<Environment>(),
            Arg.Any<Service>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithComplexHierarchy_SerializesAllServices()
    {
        // Arrange
        var project = Project.Create("Complex Project");
        await _projectRepository.AddAsync(project, CancellationToken.None);
        await _context.SaveChangesAsync();

        var dev = Environment.Create(project.Id, "Dev");
        var staging = Environment.Create(project.Id, "Staging");

        await _environmentRepository.AddAsync(dev, CancellationToken.None);
        await _environmentRepository.AddAsync(staging, CancellationToken.None);
        await _context.SaveChangesAsync();

        var devService1 = Service.Create(dev.Id, "API", ServiceType.Process, ExposureMode.Internal);
        var devService2 = Service.Create(dev.Id, "DB", ServiceType.Process, ExposureMode.Internal);
        var stagingService1 = Service.Create(staging.Id, "API", ServiceType.Process, ExposureMode.Internal);
        var stagingService2 = Service.Create(staging.Id, "Cache", ServiceType.Process, ExposureMode.Internal);

        await _serviceRepository.AddAsync(devService1, CancellationToken.None);
        await _serviceRepository.AddAsync(devService2, CancellationToken.None);
        await _serviceRepository.AddAsync(stagingService1, CancellationToken.None);
        await _serviceRepository.AddAsync(stagingService2, CancellationToken.None);
        await _context.SaveChangesAsync();

        var @event = new ManifestDirtyEvent();

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        await _mockSerializer.Received(4).WriteServiceAsync(
            Arg.Any<Project>(),
            Arg.Any<Environment>(),
            Arg.Any<Service>(),
            Arg.Any<CancellationToken>());
    }
}
