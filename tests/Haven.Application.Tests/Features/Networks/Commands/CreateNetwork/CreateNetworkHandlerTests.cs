using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.Networks.Commands.CreateNetwork;
using Haven.Domain;
using NSubstitute;
using Shouldly;

namespace Haven.Application.Tests.Features.Networks.Commands.CreateNetwork;

[Category("Unit")]
public sealed class CreateNetworkHandlerTests
{
    private INetworkRepository _networkRepository = null!;
    private IUnitOfWork _unitOfWork = null!;
    private CreateNetworkHandler _sut = null!;

    [SetUp]
    public void Setup()
    {
        _networkRepository = Substitute.For<INetworkRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _sut = new CreateNetworkHandler(_networkRepository, _unitOfWork);
    }

    [Test]
    public async Task Handle_WithProjectAndEnvironmentIds_CreatesProjectEnvironmentNetwork()
    {
        var projectId = Guid.NewGuid();
        var environmentId = Guid.NewGuid();
        var networkName = "test-network";
        var command = new CreateNetworkCommand(networkName, projectId, environmentId);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBe(Guid.Empty);

        await _networkRepository.Received(1).AddAsync(
            Arg.Is<Haven.Domain.Aggregates.Network>(n =>
                n.Name == networkName &&
                n.Type == NetworkType.ProjectEnvironment &&
                n.ProjectId == projectId &&
                n.EnvironmentId == environmentId),
            Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithoutProjectAndEnvironmentIds_CreatesSharedNetwork()
    {
        var networkName = "shared-network";
        var command = new CreateNetworkCommand(networkName);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();

        await _networkRepository.Received(1).AddAsync(
            Arg.Is<Haven.Domain.Aggregates.Network>(n =>
                n.Name == networkName &&
                n.Type == NetworkType.Shared &&
                n.ProjectId == null &&
                n.EnvironmentId == null),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithMetadata_PreservesMetadata()
    {
        var projectId = Guid.NewGuid();
        var environmentId = Guid.NewGuid();
        var metadata = "{\"key\": \"value\"}";
        var command = new CreateNetworkCommand("test-network", projectId, environmentId, metadata);

        await _sut.Handle(command, CancellationToken.None);

        await _networkRepository.Received(1).AddAsync(
            Arg.Is<Haven.Domain.Aggregates.Network>(n => n.Metadata == metadata),
            Arg.Any<CancellationToken>());
    }
}
