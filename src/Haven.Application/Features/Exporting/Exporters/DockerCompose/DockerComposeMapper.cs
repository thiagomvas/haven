using Haven.Domain.Enums;

using Riok.Mapperly.Abstractions;

namespace Haven.Application.Features.Exporting.Exporters.DockerCompose;

[Mapper]
public static partial class DockerComposeMapper
{
    [MapperIgnoreTarget(nameof(ComposeService.Build))]
    [MapperIgnoreTarget(nameof(ComposeService.Volumes))]
    [MapProperty(nameof(ServiceExportModel.EnvironmentVariables), nameof(ComposeService.Environment))]
    private static partial ComposeService ToComposeServicePartial(this ServiceExportModel serviceModel);

    public static ComposeService ToComposeService(this ServiceExportModel serviceModel)
    {
        var composeService = serviceModel.ToComposeServicePartial();
        composeService.Build = serviceModel.DockerfilePath is not null
            ? new ComposeBuild { Dockerfile = serviceModel.DockerfilePath }
            : null;

        composeService.Volumes = serviceModel.Volumes.Count > 0
            ? serviceModel.Volumes.Select(ToComposeVolumeMount).ToList()
            : null;

        return composeService;
    }

    public static ComposeFile ToComposeFile(this ServiceExportModel serviceModel) =>
        new[] { serviceModel }.ToComposeFile();

    public static ComposeFile ToComposeFile(this IReadOnlyList<ServiceExportModel> serviceModels)
    {
        var composeFile = new ComposeFile();

        foreach (var serviceModel in serviceModels)
        {
            composeFile.Services[serviceModel.Name] = serviceModel.ToComposeService();

            foreach (var volume in serviceModel.Volumes.Where(v => v.Type == VolumeType.Named))
            {
                composeFile.Volumes[volume.Source] = null;
            }
        }

        return composeFile;
    }

    private static string ToComposeVolumeMount(ServiceExportVolumeModel volume) =>
        volume.ReadOnly ? $"{volume.Source}:{volume.Target}:ro" : $"{volume.Source}:{volume.Target}";
}
