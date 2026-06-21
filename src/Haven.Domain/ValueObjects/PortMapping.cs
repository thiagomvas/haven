using System.Text.Json.Serialization;

namespace Haven.Domain.ValueObjects;

public class PortMapping : ValueObject
{
    public int? HostPort { get; init; }
    public int ContainerPort { get; init; }
    public string? HostIp { get; init; }

    [JsonConstructor]
    public PortMapping(int? hostPort, int containerPort, string? hostIp = null)
    {
        HostPort = hostPort;
        ContainerPort = containerPort;
        HostIp = hostIp;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return HostPort;
        yield return ContainerPort;
        yield return HostIp;
    }

    public override string ToString() => HostPort.HasValue
        ? $"{HostIp}:{HostPort}->{ContainerPort}"
        : $"{ContainerPort}";
}