using Haven.Domain;
using Haven.Domain.Entities;
using Shouldly;

namespace Haven.Domain.Tests.Entities;

[TestFixture]
[Category("Unit")]
public sealed class ServiceNetworkTests
{
    [Test]
    public void ConnectToNetwork_AddsNetworkConnection()
    {
        var service = Service.Create(Guid.NewGuid(), "test-service", ServiceType.DockerImage, ExposureMode.None);
        var networkId = Guid.NewGuid();

        service.ConnectToNetwork(networkId);

        service.ServiceNetworks.Count.ShouldBe(1);
        service.ServiceNetworks[0].NetworkId.ShouldBe(networkId);
        service.ServiceNetworks[0].ServiceId.ShouldBe(service.Id);
    }

    [Test]
    public void ConnectToNetwork_MultipleNetworks_AddsEach()
    {
        var service = Service.Create(Guid.NewGuid(), "test-service", ServiceType.DockerImage, ExposureMode.None);
        var networkId1 = Guid.NewGuid();
        var networkId2 = Guid.NewGuid();

        service.ConnectToNetwork(networkId1);
        service.ConnectToNetwork(networkId2);

        service.ServiceNetworks.Count.ShouldBe(2);
        service.ServiceNetworks.Select(sn => sn.NetworkId).ShouldContain(networkId1);
        service.ServiceNetworks.Select(sn => sn.NetworkId).ShouldContain(networkId2);
    }

    [Test]
    public void ConnectToNetwork_SameNetworkTwice_DoesNotDuplicate()
    {
        var service = Service.Create(Guid.NewGuid(), "test-service", ServiceType.DockerImage, ExposureMode.None);
        var networkId = Guid.NewGuid();

        service.ConnectToNetwork(networkId);
        service.ConnectToNetwork(networkId);

        service.ServiceNetworks.Count.ShouldBe(1);
    }

    [Test]
    public void DisconnectFromNetwork_RemovesConnection()
    {
        var service = Service.Create(Guid.NewGuid(), "test-service", ServiceType.DockerImage, ExposureMode.None);
        var networkId = Guid.NewGuid();

        service.ConnectToNetwork(networkId);
        service.DisconnectFromNetwork(networkId);

        service.ServiceNetworks.ShouldBeEmpty();
    }

    [Test]
    public void DisconnectFromNetwork_WithMultipleNetworks_RemovesOnly()
    {
        var service = Service.Create(Guid.NewGuid(), "test-service", ServiceType.DockerImage, ExposureMode.None);
        var networkId1 = Guid.NewGuid();
        var networkId2 = Guid.NewGuid();

        service.ConnectToNetwork(networkId1);
        service.ConnectToNetwork(networkId2);
        service.DisconnectFromNetwork(networkId1);

        service.ServiceNetworks.Count.ShouldBe(1);
        service.ServiceNetworks[0].NetworkId.ShouldBe(networkId2);
    }

    [Test]
    public void DisconnectFromNetwork_NonExistentNetwork_DoesNothing()
    {
        var service = Service.Create(Guid.NewGuid(), "test-service", ServiceType.DockerImage, ExposureMode.None);
        var networkId1 = Guid.NewGuid();
        var networkId2 = Guid.NewGuid();

        service.ConnectToNetwork(networkId1);
        service.DisconnectFromNetwork(networkId2);

        service.ServiceNetworks.Count.ShouldBe(1);
        service.ServiceNetworks[0].NetworkId.ShouldBe(networkId1);
    }
}
