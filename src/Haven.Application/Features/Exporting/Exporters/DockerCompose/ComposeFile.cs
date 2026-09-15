namespace Haven.Application.Features.Exporting.Exporters.DockerCompose;

public class ComposeFile
{
    public Dictionary<string, ComposeService> Services { get; set; } = [];
}