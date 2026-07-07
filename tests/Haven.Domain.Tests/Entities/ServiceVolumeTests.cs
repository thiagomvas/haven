using Haven.Domain;
using Haven.Domain.Entities;
using Haven.Domain.Exceptions;

using Shouldly;

namespace Haven.Domain.Tests.Entities;

[TestFixture]
[Category("Unit")]
public sealed class ServiceVolumeTests
{
    private static Service NewService() =>
        Service.Create(Guid.NewGuid(), "test-service", ServiceType.DockerImage, ExposureMode.None);

    [Test]
    public void Create_Managed_DropsSourceAndSetsFields()
    {
        var serviceId = Guid.NewGuid();

        var volume = ServiceVolume.Create(serviceId, VolumeType.Managed, "nginx-config", "/etc/nginx", source: "ignored");

        volume.ServiceId.ShouldBe(serviceId);
        volume.Type.ShouldBe(VolumeType.Managed);
        volume.Name.ShouldBe("nginx-config");
        volume.Target.ShouldBe("/etc/nginx");
        volume.Source.ShouldBeNull();
        volume.ReadOnly.ShouldBeFalse();
        volume.BackupEnabled.ShouldBeFalse();
    }

    [Test]
    public void Create_HostPath_KeepsAbsoluteSource()
    {
        var volume = ServiceVolume.Create(Guid.NewGuid(), VolumeType.HostPath, "data", "/data", source: "/srv/data", readOnly: true);

        volume.Source.ShouldBe("/srv/data");
        volume.ReadOnly.ShouldBeTrue();
    }

    [Test]
    public void Create_Named_KeepsVolumeName()
    {
        var volume = ServiceVolume.Create(Guid.NewGuid(), VolumeType.Named, "cache", "/var/cache", source: "cache-vol");

        volume.Source.ShouldBe("cache-vol");
    }

    [Test]
    public void Create_TrimsWhitespace()
    {
        var volume = ServiceVolume.Create(Guid.NewGuid(), VolumeType.Named, "  cache  ", "  /var/cache  ", source: "  cache-vol  ");

        volume.Name.ShouldBe("cache");
        volume.Target.ShouldBe("/var/cache");
        volume.Source.ShouldBe("cache-vol");
    }

    [Test]
    public void Create_MissingName_Throws()
    {
        Should.Throw<ValidationException>(() =>
            ServiceVolume.Create(Guid.NewGuid(), VolumeType.Managed, "  ", "/etc/nginx"));
    }

    [Test]
    public void Create_MissingTarget_Throws()
    {
        Should.Throw<ValidationException>(() =>
            ServiceVolume.Create(Guid.NewGuid(), VolumeType.Managed, "config", "  "));
    }

    [Test]
    public void Create_RelativeTarget_Throws()
    {
        Should.Throw<ValidationException>(() =>
            ServiceVolume.Create(Guid.NewGuid(), VolumeType.Managed, "config", "etc/nginx"));
    }

    [Test]
    public void Create_HostPath_WithoutSource_Throws()
    {
        Should.Throw<ValidationException>(() =>
            ServiceVolume.Create(Guid.NewGuid(), VolumeType.HostPath, "data", "/data"));
    }

    [Test]
    public void Create_HostPath_RelativeSource_Throws()
    {
        Should.Throw<ValidationException>(() =>
            ServiceVolume.Create(Guid.NewGuid(), VolumeType.HostPath, "data", "/data", source: "relative/path"));
    }

    [Test]
    public void Create_Named_WithoutSource_Throws()
    {
        Should.Throw<ValidationException>(() =>
            ServiceVolume.Create(Guid.NewGuid(), VolumeType.Named, "cache", "/var/cache"));
    }

    [Test]
    public void AddVolume_AddsToService()
    {
        var service = NewService();

        var volume = service.AddVolume(VolumeType.Managed, "nginx-config", "/etc/nginx");

        service.Volumes.Count.ShouldBe(1);
        service.Volumes[0].ShouldBe(volume);
        volume.ServiceId.ShouldBe(service.Id);
    }

    [Test]
    public void AddVolume_MultipleVolumes_AddsEach()
    {
        var service = NewService();

        service.AddVolume(VolumeType.Managed, "config", "/etc/nginx");
        service.AddVolume(VolumeType.Named, "cache", "/var/cache", source: "cache-vol");

        service.Volumes.Count.ShouldBe(2);
    }

    [Test]
    public void UpdateVolume_ChangesFields()
    {
        var service = NewService();
        var volume = service.AddVolume(VolumeType.HostPath, "data", "/data", source: "/srv/old");

        service.UpdateVolume(volume, name: default, source: "/srv/new", target: "/data2", readOnly: true, backupEnabled: true);

        volume.Source.ShouldBe("/srv/new");
        volume.Target.ShouldBe("/data2");
        volume.ReadOnly.ShouldBeTrue();
        volume.BackupEnabled.ShouldBeTrue();
    }

    [Test]
    public void UpdateVolume_InvalidState_Throws()
    {
        var service = NewService();
        var volume = service.AddVolume(VolumeType.HostPath, "data", "/data", source: "/srv/old");

        Should.Throw<ValidationException>(() =>
            service.UpdateVolume(volume, name: default, source: "relative", target: default, readOnly: default, backupEnabled: default));
    }

    [Test]
    public void UpdateVolume_ForeignVolume_Throws()
    {
        var service = NewService();
        var foreign = ServiceVolume.Create(Guid.NewGuid(), VolumeType.Managed, "config", "/etc/nginx");

        Should.Throw<ValidationException>(() =>
            service.UpdateVolume(foreign, name: "x", source: default, target: default, readOnly: default, backupEnabled: default));
    }

    [Test]
    public void RemoveVolume_RemovesFromService()
    {
        var service = NewService();
        var volume = service.AddVolume(VolumeType.Managed, "config", "/etc/nginx");

        service.RemoveVolume(volume);

        service.Volumes.ShouldBeEmpty();
    }
}
