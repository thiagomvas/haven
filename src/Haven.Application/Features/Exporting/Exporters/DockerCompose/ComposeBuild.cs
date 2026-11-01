using YamlDotNet.Serialization;

namespace Haven.Application.Features.Exporting.Exporters.DockerCompose;

public class ComposeBuild
{
    public string? Context { get; set; }
    public string? Dockerfile { get; set; }

    [YamlMember(Alias = "dockerfile_inline", ApplyNamingConventions = false)]
    public string? DockerfileInline { get; set; }

    public Dictionary<string, string>? Args { get; set; }
}
