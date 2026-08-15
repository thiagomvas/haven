using Haven.Application.Common.Interfaces.Deployment;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;
using Haven.Infrastructure.Deployment;

using NSubstitute;

using Shouldly;

using Environment = Haven.Domain.Aggregates.Environment;

namespace Haven.Infrastructure.Tests.Deployment;

[Category("Unit")]
public sealed class DeployServiceFactoryTests
{
    private DeployServiceFactory _sut = null!;
    private IDeployService _mockDockerService = null!;

    [SetUp]
    public void Setup()
    {
        _mockDockerService = Substitute.For<IDeployService>();
        _mockDockerService.CanHandle(Arg.Any<IDeployableContainer>()).Returns(true);

        _sut = new DeployServiceFactory([_mockDockerService]);
    }

    [Test]
    public void Create_WhenNoDeployServiceCanHandleTheContainer_ShouldReturnNull()
    {
        _mockDockerService.CanHandle(Arg.Any<IDeployableContainer>()).Returns(false);
        var service = CreateService();

        var result = _sut.Create(service);

        result.ShouldBeNull();
    }

    [Test]
    public void Create_WhenADeployServiceCanHandleTheContainer_ShouldReturnIt()
    {
        var service = CreateService();

        var result = _sut.Create(service);

        result.ShouldNotBeNull();
        result.ShouldBe(_mockDockerService);
    }

    [Test]
    public void Create_WhenNoMatchingDeployServiceExists_ShouldReturnNull()
    {
        var factory = new DeployServiceFactory([]);
        var service = CreateService();

        var result = factory.Create(service);

        result.ShouldBeNull();
    }

    [Test]
    public void Create_WithMultipleDeployServices_ShouldReturnFirstMatch()
    {
        var mockOtherService = Substitute.For<IDeployService>();
        mockOtherService.CanHandle(Arg.Any<IDeployableContainer>()).Returns(false);

        var factory = new DeployServiceFactory([mockOtherService, _mockDockerService]);
        var service = CreateService();

        var result = factory.Create(service);

        result.ShouldBe(_mockDockerService);
        result.ShouldNotBe(mockOtherService);
    }

    [Test]
    public void Create_ForASidecarContainer_DispatchesThroughTheSameFactory()
    {
        var sidecar = Sidecar.Create("traefik", SidecarKind.Traefik, sourceConfig: new DockerConfig { Image = "traefik:latest" });

        var result = _sut.Create(sidecar);

        result.ShouldBe(_mockDockerService);
    }

    private static Service CreateService()
    {
        var project = Project.Create("TestProject", description: "A test project");
        var environment = project.AddEnvironment("dev", description: "Development");
        var dockerConfig = new DockerConfig { Image = "myapp:latest" };
        return project.AddService(environment.Id, "test-service", ServiceType.DockerImage, ExposureMode.Internal, sourceConfig: dockerConfig);
    }
}
