using System.Text.Json.Serialization;

namespace Haven.Domain.ValueObjects;

public class PortMapping : ValueObject
{
    public int? HostPort { get; init; }
    public int ContainerPort { get; init; }

    [JsonConstructor]
    public PortMapping(int? hostPort, int containerPort)
    {
        HostPort = hostPort;
        ContainerPort = containerPort;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return HostPort;
        yield return ContainerPort;
    }

    public override string ToString() => HostPort.HasValue
        ? $"{HostPort}->{ContainerPort}"
        : $"{ContainerPort}";
}
