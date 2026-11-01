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

    [Test]
    public void ToExportModel_DockerSettings_AreCarriedOver()
    {
        var service = Service.Create(Guid.NewGuid(), "svc", ServiceType.DockerImage, ExposureMode.External,
            sourceConfig: new DockerConfig
            {
                Image = "nginx",
                Ports = ["8080:80"],
                CommandArgs = ["--a", "--b"],
                RestartPolicy = RestartPolicy.OnFailure,
                MemoryLimit = 1024,
                MemorySwap = 0,
                NanoCpus = 500_000_000
            });

        var model = service.ToExportModel([], "/data/volumes");

        model.Ports.ShouldBe(["8080:80"]);
        model.Command.ShouldBe(["--a", "--b"]);
        model.Restart.ShouldBe("on-failure");
        model.MemoryLimit.ShouldBe(1024);
        model.MemorySwap.ShouldBeNull();
        model.MemoryReservation.ShouldBeNull();
        model.Cpus.ShouldBe(0.5m);
    }

    [TestCase(ExposureMode.None, new string[0])]
    [TestCase(ExposureMode.Internal, new[] { "127.0.0.1:8080:80" })]
    [TestCase(ExposureMode.External, new[] { "8080:80" })]
    public void ToExportModel_Ports_FollowExposureMode(ExposureMode mode, string[] expected)
    {
        var service = Service.Create(Guid.NewGuid(), "svc", ServiceType.DockerImage, mode,
            sourceConfig: new DockerConfig { Image = "nginx", Ports = ["8080:80"] });

        service.ToExportModel([], "/data/volumes").Ports.ShouldBe(expected);
    }

    [Test]
    public void ToExportModel_CustomExposure_KeepsExplicitHostIp()
    {
        var service = Service.Create(Guid.NewGuid(), "svc", ServiceType.DockerImage, ExposureMode.Custom,
            sourceConfig: new DockerConfig { Image = "nginx", Ports = ["10.0.0.5:8080:80"] });

        service.ToExportModel([], "/data/volumes").Ports.ShouldBe(["10.0.0.5:8080:80"]);
    }

    [Test]
    public void ToExportModel_RawDockerfile_ExportsContentAndBuildArgs()
    {
        var service = Service.Create(Guid.NewGuid(), "svc", ServiceType.Dockerfile, ExposureMode.None,
            sourceConfig: new DockerfileConfig
            {
                Source = DockerfileSource.Raw,
                Content = "FROM scratch",
                BuildArgs = new Dictionary<string, string> { ["A"] = "1" }
            });

        var model = service.ToExportModel([], "/data/volumes");

        model.DockerfileContent.ShouldBe("FROM scratch");
        model.DockerfilePath.ShouldBeNull();
        model.BuildArgs["A"].ShouldBe("1");
    }

    [Test]
    public void ToExportModel_GitDockerfile_BuildsGitContextAndContextRelativeDockerfile()
    {
        var service = Service.Create(Guid.NewGuid(), "svc", ServiceType.Dockerfile, ExposureMode.None,
            sourceConfig: new DockerfileConfig
            {
                Source = DockerfileSource.Git,
                Repository = "https://example.com/org/repo.git",
                Branch = "dev",
                BuildContext = "services/api",
                FilePath = "services/api/docker/Dockerfile"
            });

        var model = service.ToExportModel([], "/data/volumes");

        model.BuildContext.ShouldBe("https://example.com/org/repo.git#dev:services/api");
        model.DockerfilePath.ShouldBe("docker/Dockerfile");
    }

    [Test]
    public void ToExportModel_SecretKeys_AreSeparatedFromPlainVariables()
    {
        var service = NewDockerImageService();
        List<DomainEnvironmentVariables> variables = [new() { Key = "PASSWORD", Value = "plain" }, new() { Key = "OTHER", Value = "x" }];
        List<DomainEnvironmentVariables> secrets = [new() { Key = "PASSWORD", Value = "s3cret" }];

        var model = service.ToExportModel(variables, "/data/volumes", secrets);

        model.SecretKeys.ShouldBe(["PASSWORD"]);
        model.EnvironmentVariables.ShouldNotContainKey("PASSWORD");
        model.EnvironmentVariables["OTHER"].ShouldBe("x");
    }

    [Test]
    public void ToExportModel_VariableWithoutKey_IsIgnored()
    {
        var service = NewDockerImageService();
        List<DomainEnvironmentVariables> variables = [new() { Key = null!, Value = "orphan flag" }];

        var model = Should.NotThrow(() => service.ToExportModel(variables, "/data/volumes"));

        model.EnvironmentVariables.ShouldBeEmpty();
    }
}
