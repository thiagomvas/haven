using YamlDotNet.Serialization;

namespace Haven.Application.Features.Exporting.Exporters.DockerCompose;

public class ComposeService
{
    public string? Image { get; set; }
    public ComposeBuild? Build { get; set; }
    public List<string>? Command { get; set; }
    public string? Restart { get; set; }
    public List<string>? Ports { get; set; }
    public Dictionary<string, string?>? Environment { get; set; }
    public List<string>? Volumes { get; set; }
    public List<string>? Networks { get; set; }

    [YamlMember(Alias = "mem_limit", ApplyNamingConventions = false)]
    public long? MemLimit { get; set; }

    [YamlMember(Alias = "memswap_limit", ApplyNamingConventions = false)]
    public long? MemswapLimit { get; set; }

    [YamlMember(Alias = "mem_reservation", ApplyNamingConventions = false)]
    public long? MemReservation { get; set; }

    public decimal? Cpus { get; set; }
}
