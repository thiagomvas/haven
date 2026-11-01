using Haven.Application.Features.Exporting;
using Haven.Application.Features.Exporting.Exporters.DockerCompose;
using Haven.Domain.Enums;

using Shouldly;

namespace Haven.Application.Tests.Features.Exporting.Exporters.DockerCompose;

[Category("Unit")]
public sealed class DockerComposeMapperTests
{
    private static ServiceExportModel NewModel(string name = "svc", string? image = "nginx:latest", string? dockerfilePath = null) =>
        new()
        {
            Name = name,
            Image = image,
            DockerfilePath = dockerfilePath
        };

    [Test]
    public void ToComposeFile_ServiceWithImage_IncludesServiceWithImage()
    {
        var model = NewModel(image: "nginx:latest");

        var composeFile = model.ToComposeFile();

        composeFile.Services.ShouldContainKey("svc");
        composeFile.Services["svc"].Image.ShouldBe("nginx:latest");
        composeFile.Services["svc"].Build.ShouldBeNull();
    }

    [Test]
    public void ToComposeFile_ServiceWithDockerfilePath_SetsBuildDockerfile()
    {
        var model = NewModel(image: null, dockerfilePath: "docker/Dockerfile");

        var composeFile = model.ToComposeFile();

        composeFile.Services["svc"].Image.ShouldBeNull();
        composeFile.Services["svc"].Build.ShouldNotBeNull();
        composeFile.Services["svc"].Build!.Dockerfile.ShouldBe("docker/Dockerfile");
    }

    [Test]
    public void ToComposeFile_ServiceWithNeitherImageNorDockerfilePath_IsOmitted()
    {
        var model = NewModel(image: null, dockerfilePath: null);

        var composeFile = model.ToComposeFile();

        composeFile.Services.ShouldBeEmpty();
    }

    [Test]
    public void ToComposeFile_MultipleServices_OnlySkipsUnsupportedOnes()
    {
        var supported = NewModel(name: "supported", image: "nginx:latest");
        var unsupported = NewModel(name: "unsupported", image: null, dockerfilePath: null);

        var composeFile = new List<ServiceExportModel> { supported, unsupported }.ToComposeFile();

        composeFile.Services.Count.ShouldBe(1);
        composeFile.Services.ShouldContainKey("supported");
        composeFile.Services.ShouldNotContainKey("unsupported");
    }

    [Test]
    public void ToComposeFile_NamedVolume_AddsTopLevelDeclaration()
    {
        var model = NewModel();
        model.Volumes.Add(new ServiceExportVolumeModel { Type = VolumeType.Named, Source = "my-data", Target = "/data" });

        var composeFile = model.ToComposeFile();

        composeFile.Volumes.ShouldContainKey("my-data");
    }

    [TestCase(VolumeType.Managed)]
    [TestCase(VolumeType.HostPath)]
    public void ToComposeFile_NonNamedVolume_DoesNotAddTopLevelDeclaration(VolumeType type)
    {
        var model = NewModel();
        model.Volumes.Add(new ServiceExportVolumeModel { Type = type, Source = "/host/path", Target = "/data" });

        var composeFile = model.ToComposeFile();

        composeFile.Volumes.ShouldBeEmpty();
    }

    [Test]
    public void ToComposeService_ReadOnlyVolume_AppendsRoSuffix()
    {
        var model = NewModel();
        model.Volumes.Add(new ServiceExportVolumeModel { Type = VolumeType.Named, Source = "cache", Target = "/cache", ReadOnly = true });

        var composeService = model.ToComposeService();

        composeService.Volumes.ShouldNotBeNull().ShouldContain("cache:/cache:ro");
    }

    [Test]
    public void ToComposeService_WritableVolume_HasNoRoSuffix()
    {
        var model = NewModel();
        model.Volumes.Add(new ServiceExportVolumeModel { Type = VolumeType.Named, Source = "cache", Target = "/cache", ReadOnly = false });

        var composeService = model.ToComposeService();

        composeService.Volumes.ShouldNotBeNull().ShouldContain("cache:/cache");
    }

    [Test]
    public void ToComposeService_NoVolumes_VolumesIsNull()
    {
        var model = NewModel();

        var composeService = model.ToComposeService();

        composeService.Volumes.ShouldBeNull();
    }

    [Test]
    public void ToComposeFile_RuntimeSettings_AreMappedToComposeKeys()
    {
        var model = NewModel();
        model.Ports = ["127.0.0.1:8080:80"];
        model.Command = ["--flag", "value"];
        model.Restart = "unless-stopped";
        model.MemoryLimit = 536870912;
        model.MemorySwap = -1;
        model.MemoryReservation = 268435456;
        model.Cpus = 1.5m;

        var service = model.ToComposeFile().Services["svc"];

        service.Ports.ShouldBe(["127.0.0.1:8080:80"]);
        service.Command.ShouldBe(["--flag", "value"]);
        service.Restart.ShouldBe("unless-stopped");
        service.MemLimit.ShouldBe(536870912);
        service.MemswapLimit.ShouldBe(-1);
        service.MemReservation.ShouldBe(268435456);
        service.Cpus.ShouldBe(1.5m);
    }

    [Test]
    public void ToComposeFile_BuildSettings_AreMapped()
    {
        var model = NewModel(image: null, dockerfilePath: "Dockerfile");
        model.BuildContext = "https://example.com/repo.git#main:src";
        model.BuildArgs["VERSION"] = "1";

        var build = model.ToComposeFile().Services["svc"].Build!;

        build.Context.ShouldBe("https://example.com/repo.git#main:src");
        build.Dockerfile.ShouldBe("Dockerfile");
        build.Args!["VERSION"].ShouldBe("1");
    }

    [Test]
    public void ToComposeFile_RawDockerfileContent_IsExportedInline()
    {
        var model = NewModel(image: null);
        model.DockerfileContent = "FROM scratch";

        var build = model.ToComposeFile().Services["svc"].Build!;

        build.DockerfileInline.ShouldBe("FROM scratch");
        build.Dockerfile.ShouldBeNull();
    }

    [Test]
    public void ToComposeFile_SecretKeys_AreExportedAsReferencesNotValues()
    {
        var model = NewModel();
        model.SecretKeys.Add("DB_PASSWORD");

        var service = model.ToComposeFile().Services["svc"];

        service.Environment!["DB_PASSWORD"].ShouldBe("${DB_PASSWORD}");
    }

    [Test]
    public void ToComposeFile_AdditionalNetworks_AreDeclaredAndKeepDefaultNetwork()
    {
        var model = NewModel();
        model.Networks.Add("shared-net");

        var composeFile = model.ToComposeFile();

        composeFile.Networks.ShouldContainKey("shared-net");
        composeFile.Services["svc"].Networks.ShouldBe(["default", "shared-net"]);
    }
}
