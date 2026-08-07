using Haven.Application.Common.Interfaces.Deployment;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;
using Haven.Infrastructure.Deployment;
using Haven.Infrastructure.Persistence;
using Haven.Testing.Common;

using Microsoft.EntityFrameworkCore;

using NSubstitute;

using Shouldly;

using Environment = Haven.Domain.Aggregates.Environment;

namespace Haven.Infrastructure.Tests.Deployment;

[Category("Unit")]
public sealed class DeployServiceFactoryTests
{
    private DeployServiceFactory _sut = null!;
    private IEnumerable<IDeployService> _deployServices = null!;
    private HavenDbContext _db = null!;
    private IDeployService _mockDockerService = null!;

    [SetUp]
    public void Setup()
    {
        _db = TestDbContextFactory.CreateUnitDbContext();

        _mockDockerService = Substitute.For<IDeployService>();
        _mockDockerService.ServiceType.Returns(ServiceType.DockerImage);

        _deployServices = new[] { _mockDockerService };
        _sut = new DeployServiceFactory(_deployServices, _db);
    }

    [TearDown]
    public void TearDown()
    {
        _db?.Dispose();
    }

    [Test]
    public void Create_WhenServiceTypeIsNotDockerImage_ShouldReturnNull()
    {
        var service = CreateServiceWithType(ServiceType.Compose);

        var result = _sut.Create(service);

        result.ShouldBeNull();
    }

    [Test]
    public void Create_WhenServiceTypeIsProcess_ShouldReturnNull()
    {
        var service = CreateServiceWithType(ServiceType.Process);

        var result = _sut.Create(service);

        result.ShouldBeNull();
    }

    [Test]
    public void Create_WhenDockerImageButSourceConfigIsNull_ShouldReturnNull()
    {
        var service = CreateServiceWithType(ServiceType.DockerImage, sourceConfig: null);

        var result = _sut.Create(service);

        result.ShouldBeNull();
    }


    [Test]
    public void Create_WhenDockerImageWithValidDockerConfig_ShouldReturnDockerDeployService()
    {
        var dockerConfig = new DockerConfig { Image = "myapp:latest" };
        var service = CreateServiceWithType(ServiceType.DockerImage, sourceConfig: dockerConfig);

        var result = _sut.Create(service);

        result.ShouldNotBeNull();
        result.ShouldBe(_mockDockerService);
    }

    [Test]
    public void Create_WhenDockerImageWithValidDockerConfig_ReturnedServiceTypeShouldBeDockerImage()
    {
        var dockerConfig = new DockerConfig { Image = "myapp:latest" };
        var service = CreateServiceWithType(ServiceType.DockerImage, sourceConfig: dockerConfig);

        var result = _sut.Create(service);

        result.ShouldNotBeNull();
        result!.ServiceType.ShouldBe(ServiceType.DockerImage);
    }

    [Test]
    public void Create_WhenNoMatchingDeployServiceExists_ShouldReturnNull()
    {
        var emptyServices = Enumerable.Empty<IDeployService>();
        var factory = new DeployServiceFactory(emptyServices, _db);

        var dockerConfig = new DockerConfig { Image = "myapp:latest" };
        var service = CreateServiceWithType(ServiceType.DockerImage, sourceConfig: dockerConfig);

        var result = factory.Create(service);

        result.ShouldBeNull();
    }

    [Test]
    public void Create_WithMultipleDeployServices_ShouldReturnCorrectOneForDockerImage()
    {
        var mockComposeService = Substitute.For<IDeployService>();
        mockComposeService.ServiceType.Returns(ServiceType.Compose);

        var allServices = new[] { mockComposeService, _mockDockerService };
        var factory = new DeployServiceFactory(allServices, _db);

        var dockerConfig = new DockerConfig { Image = "myapp:latest" };
        var service = CreateServiceWithType(ServiceType.DockerImage, sourceConfig: dockerConfig);

        var result = factory.Create(service);

        result.ShouldBe(_mockDockerService);
        result.ShouldNotBe(mockComposeService);
    }

    private static Service CreateServiceWithType(
        ServiceType serviceType,
        ServiceSourceConfig? sourceConfig = null)
    {
        var project = Project.Create("TestProject", description: "A test project");
        var environment = project.AddEnvironment("dev", description: "Development");
        return project.AddService(environment.Id, "test-service", serviceType, ExposureMode.Internal, sourceConfig: sourceConfig);
    }
}