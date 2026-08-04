using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain;
using Haven.Domain.Entities;
using Haven.Domain.ValueObjects;
using Haven.Infrastructure.Deployment;

using NSubstitute;

using Shouldly;

using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Infrastructure.Tests.Deployment;

[Category("Unit")]
public sealed class DeployWebhookServiceTests
{
    private DeployWebhookService _sut = null!;
    private IDeploymentJobEnqueuer _jobEnqueuer = null!;
    private IServiceRepository _repository = null!;

    [SetUp]
    public void Setup()
    {
        _jobEnqueuer = Substitute.For<IDeploymentJobEnqueuer>();
        _repository = Substitute.For<IServiceRepository>();
        _sut = new DeployWebhookService(_jobEnqueuer, _repository);
    }

    [Test]
    public async Task TryEnqueueWithTokenAsync_WhenServiceNotFound_ShouldReturnNotFound()
    {
        const string token = "some-token";
        _repository.GetByTokenAsync(token, Arg.Any<CancellationToken>()).Returns((Service?)null);

        var result = await _sut.TryEnqueueWithTokenAsync(token, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(Error.NotFound);
        _jobEnqueuer.DidNotReceiveWithAnyArgs().EnqueueDeployment(default, default, default);
    }

    [Test]
    public async Task TryEnqueueWithTokenAsync_WhenServiceHasNoEnvironment_ShouldReturnNotFound()
    {
        const string token = "some-token";
        var service = Service.Create(Guid.NewGuid(), "svc", ServiceType.DockerImage, ExposureMode.None);

        _repository.GetByTokenAsync(token, Arg.Any<CancellationToken>()).Returns(service);

        var result = await _sut.TryEnqueueWithTokenAsync(token, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(Error.NotFound);
        _jobEnqueuer.DidNotReceiveWithAnyArgs().EnqueueDeployment(default, default, default);
    }

    [Test]
    public async Task TryEnqueueWithTokenAsync_WhenServiceHasEnvironment_ShouldEnqueueDeploymentAndReturnSuccess()
    {
        const string token = "some-token";
        var projectId = Guid.NewGuid();
        var environment = Environment.Create(projectId, "prod");
        var service = environment.AddService("svc", ServiceType.DockerImage, ExposureMode.None);

        _repository.GetByTokenAsync(token, Arg.Any<CancellationToken>()).Returns(service);

        var result = await _sut.TryEnqueueWithTokenAsync(token, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _jobEnqueuer.Received(1).EnqueueDeployment(projectId, environment.Id, service.Id);
    }

    [Test]
    public async Task TryEnqueueWithTokenAsync_ShouldPassTokenAndCancellationTokenToRepository()
    {
        const string token = "some-token";
        using var cts = new CancellationTokenSource();

        _repository.GetByTokenAsync(token, cts.Token).Returns((Service?)null);

        await _sut.TryEnqueueWithTokenAsync(token, cts.Token);

        await _repository.Received(1).GetByTokenAsync(token, cts.Token);
    }
}