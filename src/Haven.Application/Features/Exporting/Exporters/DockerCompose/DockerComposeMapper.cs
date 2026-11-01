using Haven.Domain.Enums;

using Riok.Mapperly.Abstractions;

namespace Haven.Application.Features.Exporting.Exporters.DockerCompose;

[Mapper]
public static partial class DockerComposeMapper
{
    [MapperIgnoreTarget(nameof(ComposeService.Build))]
    [MapperIgnoreTarget(nameof(ComposeService.Volumes))]
    [MapperIgnoreTarget(nameof(ComposeService.Networks))]
    [MapperIgnoreTarget(nameof(ComposeService.MemLimit))]
    [MapperIgnoreTarget(nameof(ComposeService.MemswapLimit))]
    [MapperIgnoreTarget(nameof(ComposeService.MemReservation))]
    [MapProperty(nameof(ServiceExportModel.EnvironmentVariables), nameof(ComposeService.Environment))]
    private static partial ComposeService ToComposeServicePartial(this ServiceExportModel serviceModel);

    public static ComposeService ToComposeService(this ServiceExportModel serviceModel)
    {
        var composeService = serviceModel.ToComposeServicePartial();
        composeService.Build = serviceModel.DockerfilePath is not null || serviceModel.DockerfileContent is not null
            ? new ComposeBuild
            {
                Context = serviceModel.BuildContext,
                Dockerfile = serviceModel.DockerfilePath,
                DockerfileInline = serviceModel.DockerfileContent,
                Args = serviceModel.BuildArgs.Count > 0 ? new Dictionary<string, string>(serviceModel.BuildArgs) : null
            }
            : null;

        // Secrets are exported as ${KEY} references to be filled from the shell or an .env file.
        if (serviceModel.SecretKeys.Count > 0)
        {
            composeService.Environment ??= [];
            foreach (var key in serviceModel.SecretKeys)
                composeService.Environment[key] = "${" + key + "}";
        }

        composeService.MemLimit = serviceModel.MemoryLimit;
        composeService.MemswapLimit = serviceModel.MemorySwap;
        composeService.MemReservation = serviceModel.MemoryReservation;

        if (serviceModel.Networks.Count > 0)
            composeService.Networks = ["default", .. serviceModel.Networks];

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

            foreach (var network in serviceModel.Networks)
            {
                composeFile.Networks[network] = null;
            }
        }

        return composeFile;
    }

    private static string ToComposeVolumeMount(ServiceExportVolumeModel volume) =>
        volume.ReadOnly ? $"{volume.Source}:{volume.Target}:ro" : $"{volume.Source}:{volume.Target}";
}