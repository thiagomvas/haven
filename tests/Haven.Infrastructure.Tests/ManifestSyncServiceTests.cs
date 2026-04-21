using Haven.Application.Common.Interfaces;
using Haven.Domain.Aggregates;
using Haven.Infrastructure.Persistence;
using Haven.Infrastructure.Persistence.Interceptors;
using Haven.Infrastructure.Security;
using Mediator;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Haven.Infrastructure.Tests;

[Category("Unit")]
public sealed class ManifestSyncServiceTests
{
    private ManifestSyncService _sut = null!;
    private IServiceScopeFactory _scopeFactory = null!;
    private ILogger<ManifestSyncService> _logger = null!;
    private IServiceScope _scope = null!;
    private IServiceProvider _serviceProvider = null!;
    private IManifestSerializer _serializer = null!;
    private HavenDbContext _context = null!;
    private SqliteConnection _connection = null!;

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

        _context = new HavenDbContext(options, domainEventInterceptor, encryptionService);
        _context.Database.EnsureCreated();

        _scope = Substitute.For<IServiceScope>();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _scopeFactory = Substitute.For<IServiceScopeFactory>();
        _logger = Substitute.For<ILogger<ManifestSyncService>>();
        _serializer = Substitute.For<IManifestSerializer>();

        _scopeFactory.CreateScope().Returns(_scope);
        _scope.ServiceProvider.Returns(_serviceProvider);
        _serviceProvider.GetService(typeof(IManifestSerializer)).Returns(_serializer);
        _serviceProvider.GetService(typeof(HavenDbContext)).Returns(_context);

        _sut = new ManifestSyncService(_scopeFactory, _logger);
    }

    [TearDown]
    public void TearDown()
    {
        _context?.Dispose();
        _scope?.Dispose();
        _connection?.Dispose();
    }

    [Test]
    public async Task StartAsync_ShouldReadProjectsFromSerializer()
    {
        var projects = new List<Project> { CreateProject() };
        _serializer.ReadProjectsAsync(Arg.Any<CancellationToken>()).Returns(projects);

        await _sut.StartAsync(CancellationToken.None);

        await _serializer.Received(1).ReadProjectsAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task StartAsync_ShouldDeleteExistingProjects()
    {
        var existingProject = CreateProject("Existing");
        _context.Projects.Add(existingProject);
        await _context.SaveChangesAsync();

        var newProjects = new List<Project> { CreateProject("New") };
        _serializer.ReadProjectsAsync(Arg.Any<CancellationToken>()).Returns(newProjects);

        await _sut.StartAsync(CancellationToken.None);

        var remainingProjects = await _context.Projects.ToListAsync();
        remainingProjects.ShouldHaveSingleItem();
        remainingProjects[0].Name.ShouldBe("New");
    }

    [Test]
    public async Task StartAsync_ShouldAddReadProjects()
    {
        var projects = new List<Project> { CreateProject(), CreateProject() };
        _serializer.ReadProjectsAsync(Arg.Any<CancellationToken>()).Returns(projects);

        await _sut.StartAsync(CancellationToken.None);

        var storedProjects = await _context.Projects.ToListAsync();
        storedProjects.Count.ShouldBe(2);
    }

    [Test]
    public async Task StartAsync_ShouldSaveChanges()
    {
        var projects = new List<Project> { CreateProject() };
        _serializer.ReadProjectsAsync(Arg.Any<CancellationToken>()).Returns(projects);

        await _sut.StartAsync(CancellationToken.None);

        var storedProjects = await _context.Projects.ToListAsync();
        storedProjects.ShouldNotBeEmpty();
    }

    [Test]
    public async Task StartAsync_ShouldLogInitializationMessage()
    {
        var projects = new List<Project> { CreateProject() };
        _serializer.ReadProjectsAsync(Arg.Any<CancellationToken>()).Returns(projects);

        await _sut.StartAsync(CancellationToken.None);

        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(x => x.ToString()!.Contains("Synchronizing database from manifests")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Test]
    public async Task StartAsync_ShouldLogCompletionWithProjectCount()
    {
        var projects = new List<Project> { CreateProject(), CreateProject() };
        _serializer.ReadProjectsAsync(Arg.Any<CancellationToken>()).Returns(projects);

        await _sut.StartAsync(CancellationToken.None);

        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(x => x.ToString()!.Contains("Synchronized") && x.ToString()!.Contains("2")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Test]
    public async Task StartAsync_ShouldDisposeScopeAfterCompletion()
    {
        var projects = new List<Project> { CreateProject() };
        _serializer.ReadProjectsAsync(Arg.Any<CancellationToken>()).Returns(projects);

        await _sut.StartAsync(CancellationToken.None);

        _scope.Received(1).Dispose();
    }

    [Test]
    public async Task StartAsync_WithEmptyProjectList_ShouldDeleteAllProjects()
    {
        _context.Projects.Add(CreateProject());
        await _context.SaveChangesAsync();

        _serializer.ReadProjectsAsync(Arg.Any<CancellationToken>()).Returns(new List<Project>());

        await _sut.StartAsync(CancellationToken.None);

        var remainingProjects = await _context.Projects.ToListAsync();
        remainingProjects.ShouldBeEmpty();
    }

    [Test]
    public async Task StartAsync_ShouldRespectCancellationToken()
    {
        var cts = new CancellationTokenSource();
        var projects = new List<Project> { CreateProject() };
        _serializer.ReadProjectsAsync(Arg.Any<CancellationToken>()).Returns(projects);

        await _sut.StartAsync(cts.Token);

        await _serializer.Received(1).ReadProjectsAsync(Arg.Is<CancellationToken>(ct => ct == cts.Token));
    }

    [Test]
    public async Task StopAsync_ShouldReturnCompletedTask()
    {
        var result = _sut.StopAsync(CancellationToken.None);

        result.IsCompletedSuccessfully.ShouldBeTrue();
        await result;
    }

    private static Project CreateProject(string name = "TestProject")
    {
        return Project.Create(name, "TestKey");
    }
}
