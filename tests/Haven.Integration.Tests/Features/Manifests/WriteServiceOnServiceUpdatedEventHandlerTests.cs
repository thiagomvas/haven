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
public class WriteServiceOnServiceUpdatedEventHandlerTests
{
    private HavenDbContext _context = null!;
    private IProjectRepository _projectRepository = null!;
    private IServiceRepository _serviceRepository = null!;
    private IManifestSerializer<Service> _mockSerializer = null!;
    private WriteServiceOnServiceUpdatedEventHandler _handler = null!;

    [SetUp]
    public async Task SetUp()
    {
        _context = CreateDbContext();
        await _context.Database.EnsureCreatedAsync();

        _projectRepository = new ProjectRepository(_context);
        _serviceRepository = new ServiceRepository(_context);
        _mockSerializer = Substitute.For<IManifestSerializer<Service>>();
        _handler = new WriteServiceOnServiceUpdatedEventHandler(_mockSerializer, _serviceRepository);
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
        var environment = project.AddEnvironment("Test Environment");
        var service = environment.AddService("old-name", ServiceType.Process, ExposureMode.Internal);
        await _context.SaveChangesAsync();

        var @event = new ServiceUpdatedEvent(service.Id, "old-name", "new-name");

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        Received.InOrder(async () =>
        {
            await _mockSerializer.RenameAsync(Arg.Any<Service>(), "old-name", "new-name", Arg.Any<CancellationToken>());
            await _mockSerializer.WriteAsync(Arg.Any<Service>(), Arg.Any<CancellationToken>());
        });
    }

    [Test]
    public async Task Handle_WithoutNameChange_SkipsRenameAndCallsWriteAsync()
    {
        // Arrange
        var project = Project.Create("Test Project");
        await _projectRepository.AddAsync(project, CancellationToken.None);
        var environment = project.AddEnvironment("Test Environment");
        var service = environment.AddService("same-name", ServiceType.Process, ExposureMode.Internal);
        await _context.SaveChangesAsync();

        var @event = new ServiceUpdatedEvent(service.Id, "same-name", "same-name");

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        await _mockSerializer.DidNotReceive().RenameAsync(Arg.Any<Service>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _mockSerializer.Received(1).WriteAsync(Arg.Any<Service>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithNonexistentService_DoesNotCallAnyMethod()
    {
        // Arrange
        var @event = new ServiceUpdatedEvent(Guid.NewGuid(), "old-name", "new-name");

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        await _mockSerializer.DidNotReceive().RenameAsync(Arg.Any<Service>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _mockSerializer.DidNotReceive().WriteAsync(Arg.Any<Service>(), Arg.Any<CancellationToken>());
    }
}
