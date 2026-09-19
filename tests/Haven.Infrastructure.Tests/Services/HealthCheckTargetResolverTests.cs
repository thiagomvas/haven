using Docker.DotNet.Models;

using Haven.Infrastructure.Services;

using Shouldly;

namespace Haven.Infrastructure.Tests.Services;

[Category("Unit")]
public sealed class HealthCheckTargetResolverTests
{
    private static ContainerInspectResponse Inspect(string name, bool running, IEnumerable<string> networks, IEnumerable<string> ports) =>
        new()
        {
            Name = name,
            State = new ContainerState { Running = running },
            NetworkSettings = new NetworkSettings
            {
                Networks = networks.ToDictionary(n => n, _ => new EndpointSettings()),
                Ports = ports.ToDictionary(p => p, _ => (IList<PortBinding>)new List<PortBinding>())
            }
        };

    [Test]
    public void FromInspect_StripsLeadingSlashAndPicksLowestPort()
    {
        var target = HealthCheckTargetResolver.FromInspect(Inspect("/haven-p-e-api", true, ["haven-p-e"], ["9000/tcp", "8080/tcp"]));

        target.ContainerName.ShouldBe("haven-p-e-api");
        target.Port.ShouldBe(8080);
        target.IsRunning.ShouldBeTrue();
    }

    [Test]
    public void FromInspect_PrefersHavenEnvironmentNetworkOverSystemAndBuiltIns()
    {
        var target = HealthCheckTargetResolver.FromInspect(
            Inspect("/c", true, ["bridge", "haven-system", "shared-net", "haven-proj-env"], []));

        target.NetworkName.ShouldBe("haven-proj-env");
    }

    [Test]
    public void FromInspect_FallsBackToAnyUserDefinedNetwork()
    {
        var target = HealthCheckTargetResolver.FromInspect(Inspect("/c", true, ["bridge", "custom"], []));

        target.NetworkName.ShouldBe("custom");
    }

    [Test]
    public void FromInspect_OnlyDefaultBridge_HasNoNetwork()
    {
        var target = HealthCheckTargetResolver.FromInspect(Inspect("/c", false, ["bridge"], []));

        target.NetworkName.ShouldBeNull();
        target.IsRunning.ShouldBeFalse();
        target.Port.ShouldBeNull();
    }
}
