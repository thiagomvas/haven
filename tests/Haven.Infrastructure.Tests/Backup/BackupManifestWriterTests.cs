using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Configuration;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Infrastructure.Backup;
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
using NSubstitute.ExceptionExtensions;

using Shouldly;

using Environment = Haven.Domain.Aggregates.Environment;

namespace Haven.Infrastructure.Tests.Backup;

[TestFixture]
[Category("Integration")]
public sealed class BackupManifestWriterTests : IDisposable
{
    private HavenDbContext _context = null!;
    private SqliteConnection _connection = null!;
    private BackupManifestWriter _sut = null!;
    private string _outputDirectory = null!;
    private ILogger<BackupManifestWriter> _logger = null!;

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

        _outputDirectory = Path.Combine(Path.GetTempPath(), $"haven-backup-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_outputDirectory);

        _logger = Substitute.For<ILogger<BackupManifestWriter>>();

        var projectRepo = Substitute.For<IProjectRepository>();
        var environmentRepo = Substitute.For<IEnvironmentRepository>();

        var volumesOptions = Substitute.For<IOptionsMonitor<VolumesOptions>>();
        volumesOptions.CurrentValue.Returns(new VolumesOptions());

        IManifestEntitySerializer[] serializers =
        [
            new ProjectManifestSerializer(Substitute.For<ILogger<ProjectManifestSerializer>>()),
            new EnvironmentManifestSerializer(projectRepo, Substitute.For<ILogger<EnvironmentManifestSerializer>>()),
            new ServiceManifestSerializer(environmentRepo, volumesOptions, Substitute.For<ILogger<ServiceManifestSerializer>>()),
            new NetworkManifestSerializer(Substitute.For<ILogger<NetworkManifestSerializer>>()),
        ];

        _sut = new BackupManifestWriter(serializers, _context, _logger);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_outputDirectory))
            Directory.Delete(_outputDirectory, recursive: true);

        _context?.Dispose();
        _connection?.Dispose();
    }

    public void Dispose() => TearDown();

    [Test(Description = "No files should be created when the database has no projects or networks")]
    public async Task WriteAllAsync_WithEmptyDatabase_WritesNoFiles()
    {
        await _sut.WriteAllAsync(_outputDirectory, CancellationToken.None);

        var files = Directory.GetFiles(_outputDirectory, "*", SearchOption.AllDirectories);
        files.ShouldBeEmpty();
    }

    [Test(Description = "A project.yaml file should be created under projects/{name}/ for a persisted project")]
    public async Task WriteAllAsync_WithSingleProject_WritesProjectManifest()
    {
        var project = Project.Create("MyProject", description: "A project");
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        await _sut.WriteAllAsync(_outputDirectory, CancellationToken.None);

        var expectedPath = Path.Combine(_outputDirectory, "projects", "MyProject", "project.yaml");
        File.Exists(expectedPath).ShouldBeTrue();
    }

    [Test(Description = "The written project.yaml should contain the project name and its ID")]
    public async Task WriteAllAsync_WithSingleProject_ProjectManifestContainsExpectedContent()
    {
        var project = Project.Create("ContentProject", description: "Content check");
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        await _sut.WriteAllAsync(_outputDirectory, CancellationToken.None);

        var filePath = Path.Combine(_outputDirectory, "projects", "ContentProject", "project.yaml");
        var yaml = await File.ReadAllTextAsync(filePath);

        yaml.ShouldContain("ContentProject");
        yaml.ShouldContain(project.Id.ToString());
    }

    [Test(Description = "An environment.yaml file should be created under the project's environments/{name}/ folder")]
    public async Task WriteAllAsync_WithProjectAndEnvironment_WritesEnvironmentManifest()
    {
        var project = Project.Create("EnvProject");
        project.AddEnvironment("staging");
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        await _sut.WriteAllAsync(_outputDirectory, CancellationToken.None);

        var expectedPath = Path.Combine(_outputDirectory, "projects", "EnvProject", "environments", "staging", "environment.yaml");
        File.Exists(expectedPath).ShouldBeTrue();
    }

    [Test(Description = "A service.yaml file should be created under the environment's services/{name}/ folder")]
    public async Task WriteAllAsync_WithProjectEnvironmentAndService_WritesServiceManifest()
    {
        var project = Project.Create("ServiceProject");
        var env = project.AddEnvironment("production");
        project.AddService(env.Id, "api", ServiceType.DockerImage, ExposureMode.Internal);
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        await _sut.WriteAllAsync(_outputDirectory, CancellationToken.None);

        var expectedPath = Path.Combine(
            _outputDirectory, "projects", "ServiceProject",
            "environments", "production", "services", "api", "service.yaml");
        File.Exists(expectedPath).ShouldBeTrue();
    }

    [Test(Description = "A network.yaml file should be created under the environment folder for a ProjectEnvironment network")]
    public async Task WriteAllAsync_WithProjectEnvironmentNetwork_WritesNetworkManifest()
    {
        var project = Project.Create("NetworkProject");
        var env = project.AddEnvironment("dev");
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var network = Network.CreateProjectEnvironmentNetwork(project.Id, project.Name, env.Id, env.Name);
        _context.Networks.Add(network);
        await _context.SaveChangesAsync();

        await _sut.WriteAllAsync(_outputDirectory, CancellationToken.None);

        var expectedPath = Path.Combine(
            _outputDirectory, "projects", "NetworkProject", "environments", "dev", "network.yaml");
        File.Exists(expectedPath).ShouldBeTrue();
    }

    [Test(Description = "A project.yaml should be written for every project in the database")]
    public async Task WriteAllAsync_WithMultipleProjects_WritesAllProjectManifests()
    {
        _context.Projects.AddRange(
            Project.Create("Alpha"),
            Project.Create("Beta"),
            Project.Create("Gamma"));
        await _context.SaveChangesAsync();

        await _sut.WriteAllAsync(_outputDirectory, CancellationToken.None);

        File.Exists(Path.Combine(_outputDirectory, "projects", "Alpha", "project.yaml")).ShouldBeTrue();
        File.Exists(Path.Combine(_outputDirectory, "projects", "Beta", "project.yaml")).ShouldBeTrue();
        File.Exists(Path.Combine(_outputDirectory, "projects", "Gamma", "project.yaml")).ShouldBeTrue();
    }

    [Test(Description = "An environment.yaml should be written for every environment belonging to a project")]
    public async Task WriteAllAsync_WithProjectAndMultipleEnvironments_WritesAllEnvironmentManifests()
    {
        var project = Project.Create("MultiEnvProject");
        project.AddEnvironment("dev");
        project.AddEnvironment("staging");
        project.AddEnvironment("prod");
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        await _sut.WriteAllAsync(_outputDirectory, CancellationToken.None);

        File.Exists(Path.Combine(_outputDirectory, "projects", "MultiEnvProject", "environments", "dev", "environment.yaml")).ShouldBeTrue();
        File.Exists(Path.Combine(_outputDirectory, "projects", "MultiEnvProject", "environments", "staging", "environment.yaml")).ShouldBeTrue();
        File.Exists(Path.Combine(_outputDirectory, "projects", "MultiEnvProject", "environments", "prod", "environment.yaml")).ShouldBeTrue();
    }

    [Test(Description = "Entities whose type has no registered serializer should be silently skipped without throwing")]
    public async Task WriteAllAsync_WithNoSerializerForEntityType_SkipsEntityWithoutThrowing()
    {
        // Only register the project serializer — environments and services have no serializer
        var sutWithoutEnvSerializer = new BackupManifestWriter(
            [new ProjectManifestSerializer(Substitute.For<ILogger<ProjectManifestSerializer>>())],
            _context,
            _logger);

        var project = Project.Create("PartialProject");
        project.AddEnvironment("dev");
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        await sutWithoutEnvSerializer.WriteAllAsync(_outputDirectory, CancellationToken.None);

        // Project should exist; environment should NOT because no serializer was registered
        File.Exists(Path.Combine(_outputDirectory, "projects", "PartialProject", "project.yaml")).ShouldBeTrue();
        File.Exists(Path.Combine(_outputDirectory, "projects", "PartialProject", "environments", "dev", "environment.yaml")).ShouldBeFalse();
    }

    [Test(Description = "Networks not of type ProjectEnvironment are excluded by the query and produce no network.yaml")]
    public async Task WriteAllAsync_WithNetworkMissingProject_SkipsNetwork()
    {
        // Networks without a project or environment nav prop loaded are skipped by the writer.
        // This covers the null guard in WriteAllAsync.
        var project = Project.Create("GuardProject");
        var env = project.AddEnvironment("qa");
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        // Add a Shared network (Type != ProjectEnvironment) — it should not be queried at all.
        var sharedNetwork = Network.Create("shared-net", NetworkType.Shared);
        _context.Networks.Add(sharedNetwork);
        await _context.SaveChangesAsync();

        await _sut.WriteAllAsync(_outputDirectory, CancellationToken.None);

        // Only the project manifest should exist; no network.yaml
        File.Exists(Path.Combine(_outputDirectory, "projects", "GuardProject", "project.yaml")).ShouldBeTrue();
        var networkFiles = Directory.GetFiles(_outputDirectory, "network.yaml", SearchOption.AllDirectories);
        networkFiles.ShouldBeEmpty();
    }

    [Test(Description = "A .env.example file with real values should be written for a project's environment variables")]
    public async Task WriteAllAsync_WithProjectEnvironmentVariables_WritesEnvExampleWithValues()
    {
        var project = Project.Create("EnvVarProject");
        _context.Projects.Add(project);
        _context.EnvironmentVariables.Add(new EnvironmentVariables
        {
            ParentId = project.Id,
            ParentType = EnvironmentVariableParentType.Project,
            Key = "API_KEY",
            Value = "super-secret"
        });
        await _context.SaveChangesAsync();

        await _sut.WriteAllAsync(_outputDirectory, CancellationToken.None);

        var expectedPath = Path.Combine(_outputDirectory, "projects", "EnvVarProject", ".env.example");
        File.Exists(expectedPath).ShouldBeTrue();
        var content = await File.ReadAllTextAsync(expectedPath);
        content.ShouldContain("API_KEY=super-secret");
    }

    [Test(Description = "A .env.example file should be written under an environment's own folder, scoped to its own variables")]
    public async Task WriteAllAsync_WithEnvironmentVariables_WritesEnvExampleUnderEnvironmentFolder()
    {
        var project = Project.Create("EnvScopedProject");
        var env = project.AddEnvironment("staging");
        _context.Projects.Add(project);
        _context.EnvironmentVariables.Add(new EnvironmentVariables
        {
            ParentId = env.Id,
            ParentType = EnvironmentVariableParentType.Environment,
            Key = "DB_HOST",
            Value = "staging-db"
        });
        await _context.SaveChangesAsync();

        await _sut.WriteAllAsync(_outputDirectory, CancellationToken.None);

        var expectedPath = Path.Combine(
            _outputDirectory, "projects", "EnvScopedProject", "environments", "staging", ".env.example");
        File.Exists(expectedPath).ShouldBeTrue();
        var content = await File.ReadAllTextAsync(expectedPath);
        content.ShouldContain("DB_HOST=staging-db");

        // Should not leak into the project-level file
        var projectEnvPath = Path.Combine(_outputDirectory, "projects", "EnvScopedProject", ".env.example");
        File.Exists(projectEnvPath).ShouldBeFalse();
    }

    [Test(Description = "A .env.example file should be written under a service's own folder for its variables")]
    public async Task WriteAllAsync_WithServiceEnvironmentVariables_WritesEnvExampleUnderServiceFolder()
    {
        var project = Project.Create("SvcEnvProject");
        var env = project.AddEnvironment("prod");
        var service = project.AddService(env.Id, "api", ServiceType.DockerImage, ExposureMode.Internal);
        _context.Projects.Add(project);
        _context.EnvironmentVariables.Add(new EnvironmentVariables
        {
            ParentId = service.Id,
            ParentType = EnvironmentVariableParentType.Service,
            Key = "PORT",
            Value = "8080"
        });
        await _context.SaveChangesAsync();

        await _sut.WriteAllAsync(_outputDirectory, CancellationToken.None);

        var expectedPath = Path.Combine(
            _outputDirectory, "projects", "SvcEnvProject", "environments", "prod", "services", "api", ".env.example");
        File.Exists(expectedPath).ShouldBeTrue();
        var content = await File.ReadAllTextAsync(expectedPath);
        content.ShouldContain("PORT=8080");
    }

    [Test(Description = "No .env.example file should be written when a project has no environment variables")]
    public async Task WriteAllAsync_WithNoEnvironmentVariables_WritesNoEnvExampleFile()
    {
        var project = Project.Create("NoEnvVarsProject");
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        await _sut.WriteAllAsync(_outputDirectory, CancellationToken.None);

        var envExampleFiles = Directory.GetFiles(_outputDirectory, ".env.example", SearchOption.AllDirectories);
        envExampleFiles.ShouldBeEmpty();
    }

    [Test(Description = "An already-cancelled token should cause WriteAllAsync to throw OperationCanceledException")]
    public async Task WriteAllAsync_WithCancellationRequested_ThrowsOperationCanceledException()
    {
        var project = Project.Create("CancelProject");
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(
            () => _sut.WriteAllAsync(_outputDirectory, cts.Token));
    }

    [Test(Description = "A write that fails partway through must leave whatever was already at the target path untouched")]
    public async Task WriteAllAsync_WhenWriteFailsPartway_LeavesExistingTargetUntouched()
    {
        // Pre-existing content at the target path, simulating a previous successful write.
        var preexistingFile = Path.Combine(_outputDirectory, "marker.txt");
        await File.WriteAllTextAsync(preexistingFile, "original content");

        var project = Project.Create("FailingProject");
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var throwingProjectSerializer = Substitute.For<IManifestEntitySerializer>();
        throwingProjectSerializer.EntityType.Returns(typeof(Project));
        throwingProjectSerializer.WriteToAsync(Arg.Any<object>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("simulated write failure"));

        var failingWriter = new BackupManifestWriter([throwingProjectSerializer], _context, _logger);

        await Should.ThrowAsync<InvalidOperationException>(
            () => failingWriter.WriteAllAsync(_outputDirectory, CancellationToken.None));

        Directory.Exists(_outputDirectory).ShouldBeTrue();
        File.Exists(preexistingFile).ShouldBeTrue();
        (await File.ReadAllTextAsync(preexistingFile)).ShouldBe("original content");

        var parent = Path.GetDirectoryName(Path.GetFullPath(_outputDirectory))!;
        Directory.GetDirectories(parent, $"{Path.GetFileName(_outputDirectory)}.tmp-*").ShouldBeEmpty();
    }
}