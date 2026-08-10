using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.Networks.Commands.CreateNetwork;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;

using NSubstitute;

using Shouldly;

namespace Haven.Application.Tests.Features.Networks.Commands.CreateNetwork;

[Category("Unit")]
public sealed class CreateNetworkHandlerTests
{
    private INetworkRepository _networkRepository = null!;
    private IProjectRepository _projectRepository = null!;
    private INetworkingServiceFactory _networkingServiceFactory = null!;
    private IUnitOfWork _unitOfWork = null!;
    private CreateNetworkHandler _sut = null!;

    [SetUp]
    public void Setup()
    {
        _networkRepository = Substitute.For<INetworkRepository>();
        _projectRepository = Substitute.For<IProjectRepository>();
        _networkingServiceFactory = Substitute.For<INetworkingServiceFactory>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _networkingServiceFactory.Create(ServiceType.DockerImage).Returns((INetworkingService?)null);
        _sut = new CreateNetworkHandler(_networkRepository, _projectRepository, _networkingServiceFactory, _unitOfWork);
    }

    [Test]
    public async Task Handle_WithProjectAndEnvironmentIds_CreatesProjectEnvironmentNetwork()
    {
        var project = Project.Create("test-project");
        var environment = project.AddEnvironment("test-env");
        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);

        var networkName = "test-network";
        var command = new CreateNetworkCommand(networkName, project.Id, environment.Id);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBe(Guid.Empty);

        await _networkRepository.Received(1).AddAsync(
            Arg.Is<Network>(n =>
                n.Name == networkName &&
                n.Type == NetworkType.ProjectEnvironment &&
                n.ProjectId == project.Id &&
                n.EnvironmentId == environment.Id),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithUnknownProjectId_ReturnsNotFound()
    {
        var projectId = Guid.NewGuid();
        var environmentId = Guid.NewGuid();
        var command = new CreateNetworkCommand("test-network", projectId, environmentId);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_WithoutProjectAndEnvironmentIds_CreatesSharedNetwork()
    {
        var networkName = "shared-network";
        var command = new CreateNetworkCommand(networkName);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();

        await _networkRepository.Received(1).AddAsync(
            Arg.Is<Network>(n =>
                n.Name == networkName &&
                n.Type == NetworkType.Shared &&
                n.ProjectId == null &&
                n.EnvironmentId == null),
            Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithMetadata_PreservesMetadata()
    {
        var project = Project.Create("test-project");
        var environment = project.AddEnvironment("test-env");
        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);

        var metadata = "{\"key\": \"value\"}";
        var command = new CreateNetworkCommand("test-network", project.Id, environment.Id, metadata);

        await _sut.Handle(command, CancellationToken.None);

        await _networkRepository.Received(1).AddAsync(
            Arg.Is<Network>(n => n.Metadata == metadata),
            Arg.Any<CancellationToken>());
    }
}