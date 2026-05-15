using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Configuration;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.ValueObjects;
using Haven.Infrastructure;
using Haven.Infrastructure.Persistence;
using Haven.Infrastructure.Persistence.Interceptors;
using Haven.Infrastructure.Persistence.Manifests;
using Haven.Infrastructure.Security;
using Haven.Infrastructure.Utils;
using Mediator;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Integration.Tests;

[TestFixture]
[Category("Integration")]
public sealed class ManifestSyncOrchestratorTests : IDisposable
{
    private HavenDbContext _context = null!;
    private SqliteConnection _connection = null!;
    private ManifestSyncOrchestrator _sut = null!;
    private IManifestSerializer<Project> _projectSerializer = null!;
    private IManifestSerializer<Network> _networkSerializer = null!;
    private string _testDirectory = null!;
    private string _originalDirectory = null!;
    private ILogger<ManifestSyncOrchestrator> _logger = null!;

    [SetUp]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<HavenDbContext>()
            .UseSqlite(_connection)
            .Options;

        var encryptionService = new AesEncryptionService(
            Options.Create(new EncryptionOptions { Key = Convert.ToBase64String(new byte[32]) }));

        var mediator = Substitute.For<IMediator>();
        var domainEventInterceptor = new DomainEventInterceptor(mediator);
        var softDeleteInterceptor = new SoftDeleteInterceptor();

        _context = new HavenDbContext(options, domainEventInterceptor, softDeleteInterceptor, encryptionService);
        _context.Database.EnsureCreated();

        _testDirectory = Path.Combine(Path.GetTempPath(), $"haven-sync-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        _originalDirectory = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_testDirectory);

        var optionsMonitor = Substitute.For<IOptionsMonitor<ManifestsOptions>>();
        optionsMonitor.CurrentValue.Returns(new ManifestsOptions { ManifestsPath = _testDirectory });
        PathResolver.Initialize(optionsMonitor);

        var projectRepository = Substitute.For<IProjectRepository>();
        var environmentRepository = Substitute.For<IEnvironmentRepository>();

        _projectSerializer = new ProjectManifestSerializer(Substitute.For<ILogger<ProjectManifestSerializer>>());
        _networkSerializer = new NetworkManifestSerializer(Substitute.For<ILogger<NetworkManifestSerializer>>());
        _logger = Substitute.For<ILogger<ManifestSyncOrchestrator>>();

        _sut = new ManifestSyncOrchestrator(_projectSerializer, _networkSerializer, _context, _logger);

        // Setup repository mocks for environment serializer
        projectRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(x => _context.Projects.FirstOrDefault(p => p.Id == (Guid)x[0]));

        environmentRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(x => _context.Environments.FirstOrDefault(e => e.Id == (Guid)x[0]));
    }

    [TearDown]
    public void TearDown()
    {
        Directory.SetCurrentDirectory(_originalDirectory);
        if (Directory.Exists(_testDirectory))
            Directory.Delete(_testDirectory, recursive: true);

        _context?.Dispose();
        _connection?.Dispose();
    }

    public void Dispose()
    {
        TearDown();
    }

    [Test]
    public async Task SyncAsync_WithEmptyManifests_DeletesAllExistingProjects()
    {
        // Arrange
        var existingProject = Project.Create("ExistingProject", "To be deleted");
        _context.Projects.Add(existingProject);
        await _context.SaveChangesAsync();

        // Act
        await _sut.SyncAsync(CancellationToken.None);

        // Assert
        var projects = await _context.Projects.ToListAsync();
        projects.ShouldBeEmpty();
    }

    [Test]
    public async Task SyncAsync_WithProjectManifests_PopulatesDatabase()
    {
        // Arrange
        var project = Project.Create("TestProject", "Test project");
        await _projectSerializer.WriteAsync(project, CancellationToken.None);

        // Act
        await _sut.SyncAsync(CancellationToken.None);

        // Assert
        var syncedProjects = await _context.Projects.ToListAsync();
        syncedProjects.ShouldHaveSingleItem();
        syncedProjects[0].Name.ShouldBe("TestProject");
    }

    [Test]
    public async Task SyncAsync_WithMultipleProjects_PreservesAllProjects()
    {
        // Arrange
        var project1 = Project.Create("Project1", "First");
        var project2 = Project.Create("Project2", "Second");
        var project3 = Project.Create("Project3", "Third");

        await _projectSerializer.WriteAsync(project1, CancellationToken.None);
        await _projectSerializer.WriteAsync(project2, CancellationToken.None);
        await _projectSerializer.WriteAsync(project3, CancellationToken.None);

        // Act
        await _sut.SyncAsync(CancellationToken.None);

        // Assert
        var syncedProjects = await _context.Projects.ToListAsync();
        syncedProjects.Count.ShouldBe(3);
        syncedProjects.Select(p => p.Name).ShouldContain("Project1");
        syncedProjects.Select(p => p.Name).ShouldContain("Project2");
        syncedProjects.Select(p => p.Name).ShouldContain("Project3");
    }

    [Test]
    public async Task SyncAsync_IsDestructive_ReplacesExistingProjects()
    {
        // Arrange
        var existingProject = Project.Create("ExistingProject", "Old data");
        var existingEnv = existingProject.AddEnvironment("staging", "Old environment");
        _context.Projects.Add(existingProject);
        await _context.SaveChangesAsync();

        _context.ChangeTracker.Clear();

        var newProject = Project.Create("NewProject", "New data");
        await _projectSerializer.WriteAsync(newProject, CancellationToken.None);

        // Act
        await _sut.SyncAsync(CancellationToken.None);

        // Assert
        var projects = await _context.Projects.ToListAsync();
        projects.ShouldHaveSingleItem();
        projects[0].Name.ShouldBe("NewProject");
    }

    [Test]
    public async Task SyncAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(
            () => _sut.SyncAsync(cts.Token));
    }

}
