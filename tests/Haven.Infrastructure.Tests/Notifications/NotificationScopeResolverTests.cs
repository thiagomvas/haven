using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;
using Haven.Infrastructure.Notifications;
using Haven.Infrastructure.Persistence;
using Haven.Infrastructure.Persistence.Interceptors;
using Haven.Infrastructure.Security;

using Mediator;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Haven.Infrastructure.Tests.Notifications;

[TestFixture]
[Category("Integration")]
public sealed class NotificationScopeResolverTests : IDisposable
{
    private HavenDbContext _context = null!;
    private SqliteConnection _connection = null!;
    private NotificationScopeResolver _sut = null!;

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
        _context = new HavenDbContext(
            options,
            new DomainEventInterceptor(mediator),
            encryptionService);
        _context.Database.EnsureCreated();

        _sut = new NotificationScopeResolver(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context?.Dispose();
        _connection?.Dispose();
    }

    public void Dispose() => TearDown();

    [Test(Description = "An unsupported scope should resolve to an empty chain")]
    public async Task ResolveChainAsync_WithGlobalScope_ReturnsEmptyChain()
    {
        var chain = await _sut.ResolveChainAsync(NotificationScope.Global, Guid.NewGuid());

        chain.ShouldBeEmpty();
    }

    [Test(Description = "A project scope should resolve to itself only, with no DB lookup needed")]
    public async Task ResolveChainAsync_WithProjectScope_ReturnsOnlyProject()
    {
        var projectId = Guid.NewGuid();

        var chain = await _sut.ResolveChainAsync(NotificationScope.Project, projectId);

        chain.ShouldBe([(NotificationScope.Project, projectId)]);
    }

    [Test(Description = "An environment scope should resolve to itself plus its parent project")]
    public async Task ResolveChainAsync_WithEnvironmentScope_ReturnsEnvironmentAndProject()
    {
        var project = Project.Create("EnvChainProject");
        var environment = project.AddEnvironment("staging");
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var chain = await _sut.ResolveChainAsync(NotificationScope.Environment, environment.Id);

        chain.ShouldBe(
        [
            (NotificationScope.Environment, environment.Id),
            (NotificationScope.Project, project.Id),
        ]);
    }

    [Test(Description = "An environment scope for a non-existent environment should resolve to a default project ID alongside the environment")]
    public async Task ResolveChainAsync_WithUnknownEnvironmentId_ReturnsEnvironmentAndEmptyProjectId()
    {
        var unknownEnvironmentId = Guid.NewGuid();

        var chain = await _sut.ResolveChainAsync(NotificationScope.Environment, unknownEnvironmentId);

        chain.ShouldBe(
        [
            (NotificationScope.Environment, unknownEnvironmentId),
            (NotificationScope.Project, Guid.Empty),
        ]);
    }

    [Test(Description = "A service scope should resolve to itself, its parent environment, and its parent project")]
    public async Task ResolveChainAsync_WithServiceScope_ReturnsServiceEnvironmentAndProject()
    {
        var project = Project.Create("ServiceChainProject");
        var environment = project.AddEnvironment("production");
        var service = project.AddService(environment.Id, "api", ServiceType.DockerImage, ExposureMode.Internal);
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var chain = await _sut.ResolveChainAsync(NotificationScope.Service, service.Id);

        chain.ShouldBe(
        [
            (NotificationScope.Service, service.Id),
            (NotificationScope.Environment, environment.Id),
            (NotificationScope.Project, project.Id),
        ]);
    }

    [Test(Description = "A service scope for a non-existent service should resolve to only the service itself")]
    public async Task ResolveChainAsync_WithUnknownServiceId_ReturnsOnlyService()
    {
        var unknownServiceId = Guid.NewGuid();

        var chain = await _sut.ResolveChainAsync(NotificationScope.Service, unknownServiceId);

        chain.ShouldBe([(NotificationScope.Service, unknownServiceId)]);
    }
}