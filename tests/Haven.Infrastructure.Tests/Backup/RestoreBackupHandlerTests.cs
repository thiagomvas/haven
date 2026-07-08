using System.Reflection;

using Haven.Application.Common.Interfaces;
using Haven.Application.Configuration;
using Haven.Application.Features.Backups.Commands.RestoreBackup;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.ValueObjects;
using Haven.Infrastructure.Backup;
using Haven.Infrastructure.Persistence;
using Haven.Infrastructure.Utils;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

using Haven.Testing.Common;

using Environment = Haven.Domain.Entities.Environment;
using Service = Haven.Domain.Entities.Service;

namespace Haven.Infrastructure.Tests.Backup;

[TestFixture]
[Category("Integration")]
public sealed class RestoreBackupHandlerTests
{
    private string _sourceDir = null!;
    private string _volumesRoot = null!;
    private HavenDbContext _context = null!;
    private RestoreBackupHandler _sut = null!;
    private MethodInfo _restoreVolumeFiles = null!;

    [SetUp]
    public void SetUp()
    {
        _sourceDir = Path.Combine(Path.GetTempPath(), $"haven-restore-source-{Guid.NewGuid()}");
        _volumesRoot = Path.Combine(Path.GetTempPath(), $"haven-restore-volumes-{Guid.NewGuid()}");
        Directory.CreateDirectory(_sourceDir);
        Directory.CreateDirectory(_volumesRoot);

        _context = TestDbContextFactory.CreateUnitDbContext();

        var volumesOptions = Substitute.For<IOptionsMonitor<VolumesOptions>>();
        volumesOptions.CurrentValue.Returns(new VolumesOptions { RootPath = _volumesRoot });

        var manifestsOptions = Substitute.For<IOptionsMonitor<ManifestsOptions>>();
        manifestsOptions.CurrentValue.Returns(new ManifestsOptions());

        _sut = new RestoreBackupHandler(
            Substitute.For<IBackupManifestReader>(),
            Substitute.For<IManifestSerializer<Project>>(),
            Substitute.For<IManifestSerializer<Environment>>(),
            Substitute.For<IManifestSerializer<Network>>(),
            Substitute.For<IManifestSerializer<Service>>(),
            _context,
            Substitute.For<IBackupManifestWriter>(),
            manifestsOptions,
            volumesOptions,
            Substitute.For<ILogger<RestoreBackupHandler>>());

        _restoreVolumeFiles = typeof(RestoreBackupHandler).GetMethod(
            "RestoreVolumeFiles", BindingFlags.NonPublic | BindingFlags.Instance)!;
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
        if (Directory.Exists(_sourceDir)) Directory.Delete(_sourceDir, recursive: true);
        if (Directory.Exists(_volumesRoot)) Directory.Delete(_volumesRoot, recursive: true);
    }

    private (Project project, Environment environment, Service service, ServiceVolume volume) CreateSnapshotService(string volumeName)
    {
        var project = Project.Create("proj");
        var environment = project.AddEnvironment("dev");
        var service = environment.AddService("web", ServiceType.DockerImage, ExposureMode.External, null, new DockerConfig { Image = "nginx" });
        var volume = service.AddVolume(VolumeType.Managed, volumeName, "/data", backupEnabled: true);
        return (project, environment, service, volume);
    }

    private void WriteSnapshotVolumeFile(Project project, Environment environment, Service service, string volumeName, string fileName, string content)
    {
        var dir = Path.Combine(
            _sourceDir, "projects", project.Name,
            PathResolver.EnvironmentDirectory, environment.Name,
            PathResolver.ServiceDirectory, service.Name,
            "volumes", volumeName);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, fileName), content);
    }

    private List<string> InvokeRestoreVolumeFiles(
        Dictionary<Guid, Project> snapshotProjectById,
        Dictionary<Guid, Environment> snapshotEnvironmentById,
        Dictionary<Guid, Service> snapshotServiceById,
        Dictionary<Guid, Service> currentServiceById)
    {
        return (List<string>)_restoreVolumeFiles.Invoke(_sut, [
            _sourceDir, snapshotProjectById, snapshotEnvironmentById, snapshotServiceById, currentServiceById
        ])!;
    }

    [Test]
    public void RestoreVolumeFiles_HappyPath_CopiesFilesAndReturnsNoWarnings()
    {
        var (project, environment, service, volume) = CreateSnapshotService("config");
        WriteSnapshotVolumeFile(project, environment, service, "config", "app.conf", "hello");

        var warnings = InvokeRestoreVolumeFiles(
            new Dictionary<Guid, Project> { [project.Id] = project },
            new Dictionary<Guid, Environment> { [environment.Id] = environment },
            new Dictionary<Guid, Service> { [service.Id] = service },
            new Dictionary<Guid, Service>());

        warnings.ShouldBeEmpty();

        var destFile = Path.Combine(DockerUtils.ManagedVolumeHostPath(_volumesRoot, service.Id, volume.Id), "app.conf");
        File.Exists(destFile).ShouldBeTrue();
        File.ReadAllText(destFile).ShouldBe("hello");
    }

    [Test]
    public void RestoreVolumeFiles_WhenDestinationIsBlockedByAFile_CollectsWarningInsteadOfThrowing()
    {
        var (project, environment, service, volume) = CreateSnapshotService("config");
        WriteSnapshotVolumeFile(project, environment, service, "config", "app.conf", "hello");

        // Block the destination directory by creating a file where the directory needs to go.
        var destDir = DockerUtils.ManagedVolumeHostPath(_volumesRoot, service.Id, volume.Id);
        Directory.CreateDirectory(Path.GetDirectoryName(destDir)!);
        File.WriteAllText(destDir, "not a directory");

        List<string>? warnings = null;
        Should.NotThrow(() =>
        {
            warnings = InvokeRestoreVolumeFiles(
                new Dictionary<Guid, Project> { [project.Id] = project },
                new Dictionary<Guid, Environment> { [environment.Id] = environment },
                new Dictionary<Guid, Service> { [service.Id] = service },
                new Dictionary<Guid, Service>());
        });

        warnings.ShouldNotBeNull();
        warnings!.Count.ShouldBe(1);
        warnings[0].ShouldContain(service.Name);
        warnings[0].ShouldContain("config");
    }

    [Test]
    public void RestoreVolumeFiles_OneVolumeFails_OtherVolumesStillRestored()
    {
        var (project, environment, service, _) = CreateSnapshotService("bad-volume");
        var goodVolume = service.AddVolume(VolumeType.Managed, "good-volume", "/good", backupEnabled: true);

        WriteSnapshotVolumeFile(project, environment, service, "bad-volume", "a.txt", "a");
        WriteSnapshotVolumeFile(project, environment, service, "good-volume", "b.txt", "b");

        var badDestDir = DockerUtils.ManagedVolumeHostPath(_volumesRoot, service.Id, service.Volumes.First(v => v.Name == "bad-volume").Id);
        Directory.CreateDirectory(Path.GetDirectoryName(badDestDir)!);
        File.WriteAllText(badDestDir, "blocked");

        var warnings = InvokeRestoreVolumeFiles(
            new Dictionary<Guid, Project> { [project.Id] = project },
            new Dictionary<Guid, Environment> { [environment.Id] = environment },
            new Dictionary<Guid, Service> { [service.Id] = service },
            new Dictionary<Guid, Service>());

        warnings.Count.ShouldBe(1);

        var goodDestFile = Path.Combine(DockerUtils.ManagedVolumeHostPath(_volumesRoot, service.Id, goodVolume.Id), "b.txt");
        File.Exists(goodDestFile).ShouldBeTrue();
    }
}
