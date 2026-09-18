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
}