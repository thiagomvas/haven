namespace Haven.Application.Features.Exporting.Exporters.DockerCompose;

public class ComposeService
{
    public string? Image { get; set; }
    public ComposeBuild? Build { get; set; }
    public Dictionary<string, string?>? Environment { get; set; }
    public List<string>? Volumes { get; set; }
}