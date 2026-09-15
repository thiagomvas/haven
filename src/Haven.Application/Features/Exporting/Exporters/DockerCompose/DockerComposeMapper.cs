using Riok.Mapperly.Abstractions;

namespace Haven.Application.Features.Exporting.Exporters.DockerCompose;

[Mapper]
public static partial class DockerComposeMapper
{
    [MapperIgnoreTarget(nameof(ComposeService.Build))]
    private static partial ComposeService ToComposeServicePartial(this ServiceExportModel serviceModel);

    public static ComposeService ToComposeService(this ServiceExportModel serviceModel)
    {
        var composeService = serviceModel.ToComposeServicePartial();
        composeService.Build = serviceModel.DockerfilePath is not null
            ? new ComposeBuild { Dockerfile = serviceModel.DockerfilePath }
            : null;

        return composeService;
    }

    public static ComposeFile ToComposeFile(this ServiceExportModel serviceModel)
    {
        var composeFile = new ComposeFile();
        composeFile.Services[serviceModel.Name] = serviceModel.ToComposeService();
        return composeFile;
    }
}
