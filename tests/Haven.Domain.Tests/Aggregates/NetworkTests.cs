using Haven.Domain;
using Haven.Domain.Aggregates;

using Shouldly;

namespace Haven.Domain.Tests.Aggregates;

[TestFixture]
[Category("Unit")]
public sealed class NetworkTests
{
    [Test]
    public void CreateProjectEnvironmentNetwork_GeneratesConsistentName()
    {
        var projectId = Guid.NewGuid();
        var projectName = "MyProject";
        var environmentId = Guid.NewGuid();
        var environmentName = "staging";

        var network1 = Network.CreateProjectEnvironmentNetwork(projectId, projectName, environmentId, environmentName);
        var network2 = Network.CreateProjectEnvironmentNetwork(projectId, projectName, environmentId, environmentName);

        network1.Name.ShouldBe(network2.Name);
    }

    [Test]
    public void CreateProjectEnvironmentNetwork_NameContainsProjectAndEnvironment()
    {
        var projectId = Guid.NewGuid();
        var projectName = "MyProject";
        var environmentId = Guid.NewGuid();
        var environmentName = "staging";

        var network = Network.CreateProjectEnvironmentNetwork(projectId, projectName, environmentId, environmentName);

        network.Name.ShouldContain(projectName.ToLowerInvariant());
        network.Name.ShouldContain(environmentName.ToLowerInvariant());
    }

    [Test]
    public void CreateProjectEnvironmentNetwork_SetsNetworkType()
    {
        var network = Network.CreateProjectEnvironmentNetwork(Guid.NewGuid(), "MyProject", Guid.NewGuid(), "staging");

        network.Type.ShouldBe(NetworkType.ProjectEnvironment);
    }

    [Test]
    public void CreateProjectEnvironmentNetwork_SetsProjectAndEnvironmentIds()
    {
        var projectId = Guid.NewGuid();
        var environmentId = Guid.NewGuid();

        var network = Network.CreateProjectEnvironmentNetwork(projectId, "MyProject", environmentId, "staging");

        network.ProjectId.ShouldBe(projectId);
        network.EnvironmentId.ShouldBe(environmentId);
    }

    [Test]
    public void CreateProjectEnvironmentNetwork_WithMetadata_PreservesMetadata()
    {
        var metadata = "{\"key\": \"value\"}";

        var network = Network.CreateProjectEnvironmentNetwork(
            Guid.NewGuid(),
            "MyProject",
            Guid.NewGuid(),
            "staging",
            metadata);

        network.Metadata.ShouldBe(metadata);
    }

    [Test]
    public void Create_WithValidProjectEnvironmentScope_Succeeds()
    {
        var projectId = Guid.NewGuid();
        var environmentId = Guid.NewGuid();

        var network = Network.Create("test-network", NetworkType.ProjectEnvironment, projectId, environmentId);

        network.Name.ShouldBe("test-network");
        network.Type.ShouldBe(NetworkType.ProjectEnvironment);
        network.ProjectId.ShouldBe(projectId);
        network.EnvironmentId.ShouldBe(environmentId);
    }

    [Test]
    public void Create_WithSharedNetworkType_SucceedsWithoutProjectAndEnvironment()
    {
        var network = Network.Create("shared-network", NetworkType.Shared);

        network.Name.ShouldBe("shared-network");
        network.Type.ShouldBe(NetworkType.Shared);
        network.ProjectId.ShouldBeNull();
        network.EnvironmentId.ShouldBeNull();
    }

    [Test]
    public void Create_ProjectEnvironmentWithoutEnvironmentId_Throws()
    {
        var projectId = Guid.NewGuid();

        Should.Throw<ArgumentNullException>(() =>
            Network.Create("test-network", NetworkType.ProjectEnvironment, projectId, null));
    }

    [Test]
    public void Create_SharedNetworkWithProjectId_Throws()
    {
        var projectId = Guid.NewGuid();

        Should.Throw<ArgumentException>(() =>
            Network.Create("test-network", NetworkType.Shared, projectId, null));
    }

    [Test]
    public void AssignNetworkInfo_SetsSubnetAndGateway()
    {
        var network = Network.Create("shared-network", NetworkType.Shared);

        network.AssignNetworkInfo("172.16.5.0/24", "172.16.5.1");

        network.Subnet.ShouldBe("172.16.5.0/24");
        network.Gateway.ShouldBe("172.16.5.1");
    }

    [Test]
    public void AssignNetworkInfo_UpdatesUpdatedAt()
    {
        var network = Network.Create("shared-network", NetworkType.Shared);
        var originalUpdatedAt = network.UpdatedAt;

        network.AssignNetworkInfo("172.16.5.0/24", "172.16.5.1");

        network.UpdatedAt.ShouldBeGreaterThanOrEqualTo(originalUpdatedAt);
    }

    [Test]
    public void AssignNetworkInfo_WithEmptySubnet_Throws()
    {
        var network = Network.Create("shared-network", NetworkType.Shared);

        Should.Throw<ArgumentException>(() => network.AssignNetworkInfo("", "172.16.5.1"));
    }

    [Test]
    public void AssignNetworkInfo_WithEmptyGateway_Throws()
    {
        var network = Network.Create("shared-network", NetworkType.Shared);

        Should.Throw<ArgumentException>(() => network.AssignNetworkInfo("172.16.5.0/24", ""));
    }
}