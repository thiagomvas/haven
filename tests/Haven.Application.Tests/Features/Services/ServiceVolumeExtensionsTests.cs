using Haven.Application.Features.Services;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;

using Shouldly;

namespace Haven.Application.Tests.Features.Services;

[Category("Unit")]
public sealed class ServiceVolumeExtensionsTests
{
    private static Service NewService() =>
        Service.Create(Guid.NewGuid(), "test-service", ServiceType.DockerImage, ExposureMode.None);

    [Test]
    public void GetManagedVolume_VolumeDoesNotExist_ReturnsNotFound()
    {
        var service = NewService();

        var result = service.GetManagedVolume(Guid.NewGuid());

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("NOT_FOUND");
    }

    [Test]
    public void GetManagedVolume_NonManagedVolume_ReturnsInvalidOperation()
    {
        var service = NewService();
        var volume = service.AddVolume(VolumeType.Named, "cache", "/cache", source: "cache-vol");

        var result = service.GetManagedVolume(volume.Id);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("INVALID_OPERATION");
    }

    [Test]
    public void GetManagedVolume_ManagedVolume_ReturnsVolume()
    {
        var service = NewService();
        var volume = service.AddVolume(VolumeType.Managed, "config", "/etc/nginx");

        var result = service.GetManagedVolume(volume.Id);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(volume);
    }
}