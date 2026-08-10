using Haven.Domain.Aggregates;
using Haven.Domain.Enums;
using Haven.Infrastructure.Persistence;
using Haven.Infrastructure.Persistence.Repositories;
using Haven.Testing.Common;

using Shouldly;

namespace Haven.Infrastructure.Tests.Persistence.Repositories;

[Category("Unit")]
public sealed class ServiceRepositoryFuzzySearchTests
{
    private HavenDbContext _context = null!;
    private ServiceRepository _sut = null!;

    [SetUp]
    public void Setup()
    {
        _context = TestDbContextFactory.CreateUnitDbContext();
        _sut = new ServiceRepository(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task FuzzySearchAsync_IsCaseInsensitive_AndTranslatesAgainstSqlite()
    {
        var project = Project.Create("acme");
        var environment = project.AddEnvironment("production", "prod");
        environment.AddService("Nginx-Proxy", ServiceType.DockerImage, ExposureMode.None);

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var results = (await _sut.FuzzySearchAsync("nginx", CancellationToken.None)).ToList();

        results.ShouldHaveSingleItem();
        results.Single().Label.ShouldBe("Nginx-Proxy");
    }
}