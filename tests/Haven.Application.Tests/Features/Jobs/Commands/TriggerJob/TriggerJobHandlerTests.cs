using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Features.Jobs.Commands.TriggerJob;

using NSubstitute;

using Shouldly;

namespace Haven.Application.Tests.Features.Jobs.Commands.TriggerJob;

[Category("Unit")]
public sealed class TriggerJobHandlerTests
{
    private IJobsService _jobsService;
    private TriggerJobHandler _sut;

    [SetUp]
    public void Setup()
    {
        _jobsService = Substitute.For<IJobsService>();
        _sut = new TriggerJobHandler(_jobsService);
    }

    [Test]
    public async Task Handle_ShouldReturnSuccess_WhenServiceTriggersJob()
    {
        var command = new TriggerJobCommand("some-job-key");
        _jobsService.TriggerJobAsync(command.JobKey, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _jobsService.Received(1).TriggerJobAsync("some-job-key", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ShouldReturnFailure_WhenServiceFailsToTriggerJob()
    {
        var command = new TriggerJobCommand("missing-job-key");
        _jobsService.TriggerJobAsync(command.JobKey, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(Error.Failed));

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }
}