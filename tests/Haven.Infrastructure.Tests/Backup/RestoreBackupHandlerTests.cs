using System.Reflection;

using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Configuration;
using Haven.Application.Features.Backups.Commands.RestoreBackup;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;
using Haven.Infrastructure.Backup;
using Haven.Infrastructure.Persistence;
using Haven.Infrastructure.Utils;
using Haven.Testing.Common;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

using Environment = Haven.Domain.Aggregates.Environment;
using Service = Haven.Domain.Aggregates.Service;

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

    private IBackupManifestReader _manifestReader = null!;
    private IManifestSerializer<Project> _projectSerializer = null!;
    private IManifestSerializer<Environment> _environmentSerializer = null!;
    private IManifestSerializer<Network> _networkSerializer = null!;
    private IManifestSerializer<Sidecar> _sidecarSerializer = null!;
    private IManifestSerializer<Service> _serviceSerializer = null!;
    private IBackupManifestWriter _manifestWriter = null!;
    private IServiceCleanupJobEnqueuer _serviceCleanupJobEnqueuer = null!;

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

        _manifestReader = Substitute.For<IBackupManifestReader>();
        _manifestReader.PrepareSourceDirectoryAsync(Arg.Any<RestoreSource>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(_sourceDir);

        _projectSerializer = Substitute.For<IManifestSerializer<Project>>();
        _projectSerializer.ReadFromAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Project>)[]);

        _environmentSerializer = Substitute.For<IManifestSerializer<Environment>>();
        _environmentSerializer.ReadFromAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Environment>)[]);

        _networkSerializer = Substitute.For<IManifestSerializer<Network>>();
        _networkSerializer.ReadFromAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Network>)[]);

        _sidecarSerializer = Substitute.For<IManifestSerializer<Sidecar>>();
        _sidecarSerializer.ReadFromAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Sidecar>)[]);

        _serviceSerializer = Substitute.For<IManifestSerializer<Service>>();
        _serviceSerializer.ReadFromAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Service>)[]);

        _manifestWriter = Substitute.For<IBackupManifestWriter>();
        _serviceCleanupJobEnqueuer = Substitute.For<IServiceCleanupJobEnqueuer>();

        _sut = new RestoreBackupHandler(
            _manifestReader,
            _projectSerializer,
            _environmentSerializer,
            _networkSerializer,
            _sidecarSerializer,
            _serviceSerializer,
            _context,
            _manifestWriter,
            manifestsOptions,
            volumesOptions,
            new BackupCoordinationLock(),
            _serviceCleanupJobEnqueuer,
            Substitute.For<IEncryptionService>(),
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

    [Test]
    public async Task Handle_DryRun_NeverWritesManifests()
    {
        var command = new RestoreBackupCommand
        {
            Source = RestoreSource.FileSystem,
            SnapshotName = "snapshot",
            DryRun = true
        };

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.DryRun.ShouldBeTrue();
        await _manifestWriter.DidNotReceive().WriteAllAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test(Description = "The System network is never written to manifests, so it must never show up as deleted just because the snapshot doesn't mention it")]
    public async Task Handle_DryRun_DoesNotFlagSystemNetworkAsDeleted()
    {
        var systemNetwork = Network.CreateSystemNetwork();
        _context.Networks.Add(systemNetwork);
        await _context.SaveChangesAsync();

        // _networkSerializer keeps returning [] - the manifest never contains the System network.
        var command = new RestoreBackupCommand { Source = RestoreSource.Manifest, DryRun = true };

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Networks.Deleted.ShouldBeEmpty();
    }

    [Test(Description = "A Shared network's service attachments must be reconciled to match the manifest on restore")]
    public async Task Handle_SharedNetworkWithChangedServiceAttachment_ReconcilesServiceNetworks()
    {
        var project = Project.Create("proj");
        var environment = project.AddEnvironment("dev");
        var keptService = environment.AddService("kept", ServiceType.DockerImage, ExposureMode.Internal, null, new DockerConfig { Image = "nginx" });
        var newlyAttachedService = environment.AddService("new", ServiceType.DockerImage, ExposureMode.Internal, null, new DockerConfig { Image = "nginx" });
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var sharedNetwork = Network.Create("shared", NetworkType.Shared);
        _context.Networks.Add(sharedNetwork);
        await _context.SaveChangesAsync();

        // Currently only "kept" is attached; the snapshot below attaches "new" instead.
        _context.ServiceNetworks.Add(ServiceNetwork.Create(keptService.Id, sharedNetwork.Id));
        await _context.SaveChangesAsync();

        var snapshotNetwork = Network.Reconstitute(
            sharedNetwork.Id, sharedNetwork.Name, NetworkType.Shared, null,
            projectId: null, environmentId: null, DateTime.UtcNow, DateTime.UtcNow,
            serviceNetworks: [ServiceNetwork.Create(newlyAttachedService.Id, sharedNetwork.Id)]);

        _networkSerializer.ReadFromAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Network>)[snapshotNetwork]);
        _projectSerializer.ReadFromAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Project>)[project]);
        _environmentSerializer.ReadFromAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Environment>)[environment]);
        _serviceSerializer.ReadFromAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Service>)[keptService, newlyAttachedService]);

        var command = new RestoreBackupCommand { Source = RestoreSource.Manifest, DryRun = false };

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();

        var assignments = await _context.ServiceNetworks
            .Where(sn => sn.NetworkId == sharedNetwork.Id)
            .Select(sn => sn.ServiceId)
            .ToListAsync();

        assignments.ShouldBe([newlyAttachedService.Id]);
    }

    [Test]
    public async Task Handle_ServiceRemovedByRestore_EnqueuesDeploymentCleanup()
    {
        var project = Project.Create("proj");
        var environment = project.AddEnvironment("dev");
        var service = environment.AddService("web", ServiceType.DockerImage, ExposureMode.External, "web-alias", new DockerConfig { Image = "nginx" });

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        _projectSerializer.ReadFromAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Project>)[project]);
        _environmentSerializer.ReadFromAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Environment>)[environment]);
        // Service serializer keeps returning [] (snapshot no longer contains this service).

        var command = new RestoreBackupCommand { Source = RestoreSource.Manifest, DryRun = false };

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        (await _context.Services.AnyAsync(s => s.Id == service.Id)).ShouldBeFalse();

        _serviceCleanupJobEnqueuer.Received(1).EnqueueCleanup(Arg.Is<ServiceCleanupInfo>(i =>
            i.ServiceId == service.Id && i.ServiceName == "web" && i.Type == ServiceType.DockerImage));
    }

    [Test]
    public async Task Handle_ServiceRemovedByRestore_RemovesOrphanedEnvironmentVariables()
    {
        var project = Project.Create("proj");
        var environment = project.AddEnvironment("dev");
        var service = environment.AddService("web", ServiceType.DockerImage, ExposureMode.External, null, new DockerConfig { Image = "nginx" });

        _context.Projects.Add(project);
        _context.EnvironmentVariables.Add(new EnvironmentVariables
        {
            ParentId = service.Id,
            ParentType = EnvironmentVariableParentType.Service,
            Key = "FOO",
            Value = "bar"
        });
        await _context.SaveChangesAsync();

        _projectSerializer.ReadFromAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Project>)[project]);
        _environmentSerializer.ReadFromAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Environment>)[environment]);
        // Service serializer keeps returning [] (snapshot no longer contains this service).

        var command = new RestoreBackupCommand { Source = RestoreSource.Manifest, DryRun = false };

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        (await _context.EnvironmentVariables.AnyAsync(v => v.ParentId == service.Id)).ShouldBeFalse();
    }

    [Test]
    public async Task Handle_SidecarAddedByRestore_CreatesSidecar()
    {
        var sidecar = Sidecar.Create("cache", SidecarKind.Custom, "cache", new DockerConfig { Image = "redis" });

        _sidecarSerializer.ReadFromAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Sidecar>)[sidecar]);

        var command = new RestoreBackupCommand { Source = RestoreSource.Manifest, DryRun = false };

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Sidecars.Created.ShouldContain(i => i.Id == sidecar.Id && i.Name == "cache");
        (await _context.Sidecars.AnyAsync(s => s.Id == sidecar.Id)).ShouldBeTrue();
    }

    [Test]
    public async Task Handle_SidecarUpdatedByRestore_UpdatesFieldsAndKeepsOriginalId_MatchingByKindNotId()
    {
        var sidecar = Sidecar.Create("cache", SidecarKind.Custom, "cache", new DockerConfig { Image = "redis" });
        _context.Sidecars.Add(sidecar);
        await _context.SaveChangesAsync();

        // The manifest carries no Id (sidecars are keyed by Kind on disk), so the snapshot's Id is
        // a fresh, unrelated Guid - matching must happen by Kind, and the original DB Id must survive.
        var updatedSnapshot = Sidecar.Reconstitute(
            Guid.NewGuid(), "cache", "cache-alias", SidecarKind.Custom,
            ServiceStatus.Stopped, ServiceHealth.Unknown, enabled: false,
            createdAt: sidecar.CreatedAt, updatedAt: DateTime.UtcNow,
            sourceConfig: new DockerConfig { Image = "redis:7" });

        _sidecarSerializer.ReadFromAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Sidecar>)[updatedSnapshot]);

        var command = new RestoreBackupCommand { Source = RestoreSource.Manifest, DryRun = false };

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Sidecars.Updated.ShouldContain(i => i.Id == sidecar.Id);

        var persisted = await _context.Sidecars.AsNoTracking().SingleAsync(s => s.Id == sidecar.Id);
        persisted.Alias.ShouldBe("cache-alias");
        persisted.SourceConfigJson.ShouldContain("redis:7");
        (await _context.Sidecars.CountAsync()).ShouldBe(1);
    }

    [Test]
    public async Task Handle_SidecarRemovedByRestore_RemovesSidecar()
    {
        var sidecar = Sidecar.Create("cache", SidecarKind.Custom, "cache", new DockerConfig { Image = "redis" });
        _context.Sidecars.Add(sidecar);
        await _context.SaveChangesAsync();

        // Sidecar serializer keeps returning [] (snapshot no longer contains this sidecar).

        var command = new RestoreBackupCommand { Source = RestoreSource.Manifest, DryRun = false };

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Sidecars.Deleted.ShouldContain(i => i.Id == sidecar.Id);
        (await _context.Sidecars.AnyAsync(s => s.Id == sidecar.Id)).ShouldBeFalse();
    }
}