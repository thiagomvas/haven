using Haven.Application.Mappers;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;

using Shouldly;

using DomainEnvironmentVariables = Haven.Domain.Entities.EnvironmentVariables;

namespace Haven.Application.Tests.Mappers;

[Category("Unit")]
public sealed class ServiceMapperExportTests
{
    private static Service NewDockerImageService(string image = "nginx:latest", string? alias = null) =>
        Service.Create(Guid.NewGuid(), "test-service", ServiceType.DockerImage, ExposureMode.None, alias: alias,
            sourceConfig: new DockerConfig { Image = image });

    [Test]
    public void ToExportModel_UsesAliasWhenSet()
    {
        var service = NewDockerImageService(alias: "web");

        var model = service.ToExportModel([], "/data/volumes");

        model.Name.ShouldBe("web");
    }

    [Test]
    public void ToExportModel_FallsBackToNameWhenNoAlias()
    {
        var service = NewDockerImageService();

        var model = service.ToExportModel([], "/data/volumes");

        model.Name.ShouldBe("test-service");
    }

    [Test]
    public void ToExportModel_DuplicateKeys_LastValueWins_AndDoesNotThrow()
    {
        var service = NewDockerImageService();
        List<DomainEnvironmentVariables> variables =
        [
            new() { Key = "DEBUG", Value = "false" },
            new() { Key = "DEBUG", Value = "true" }
        ];

        var model = Should.NotThrow(() => service.ToExportModel(variables, "/data/volumes"));

        model.EnvironmentVariables["DEBUG"].ShouldBe("true");
    }

    [Test]
    public void ToExportModel_ManagedVolume_ResolvesToRootedManagedPath()
    {
        var service = NewDockerImageService();
        var volume = service.AddVolume(VolumeType.Managed, "config", "/etc/nginx");

        var model = service.ToExportModel([], "/data/volumes");

        var exportedVolume = model.Volumes.ShouldHaveSingleItem();
        exportedVolume.Source.ShouldBe(Path.GetFullPath(Path.Combine("/data/volumes", service.Id.ToString(), volume.Id.ToString())));
        exportedVolume.Target.ShouldBe("/etc/nginx");
    }

    [Test]
    public void ToExportModel_NamedVolume_UsesConfiguredSource()
    {
        var service = NewDockerImageService();
        service.AddVolume(VolumeType.Named, "cache", "/cache", source: "cache-vol");

        var model = service.ToExportModel([], "/data/volumes");

        var exportedVolume = model.Volumes.ShouldHaveSingleItem();
        exportedVolume.Source.ShouldBe("cache-vol");
    }

    [Test]
    public void ToExportModel_DockerImageService_HasNoDockerfilePath()
    {
        var service = NewDockerImageService();

        var model = service.ToExportModel([], "/data/volumes");

        model.Image.ShouldBe("nginx:latest");
        model.DockerfilePath.ShouldBeNull();
    }

    [Test]
    public void ToExportModel_DockerfileService_HasNoImage()
    {
        var service = Service.Create(Guid.NewGuid(), "test-service", ServiceType.Dockerfile, ExposureMode.None,
            sourceConfig: new DockerfileConfig { Source = DockerfileSource.Git, FilePath = "docker/Dockerfile" });

        var model = service.ToExportModel([], "/data/volumes");

        model.Image.ShouldBeNull();
        model.DockerfilePath.ShouldBe("docker/Dockerfile");
    }
}
