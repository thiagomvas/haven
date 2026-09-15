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
            var composeService = serviceModel.ToComposeService();

            // Services with neither an image nor a resolvable Dockerfile path (e.g. raw-content Dockerfiles,
            // which have no on-disk file to reference) can't be represented in Compose - skip rather than
            // emit an empty, invalid service entry.
            if (composeService.Image is null && composeService.Build is null)
                continue;

            composeFile.Services[serviceModel.Name] = composeService;

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
