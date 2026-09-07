using Haven.Application.Common;
using Haven.Application.Common.Contracts;
using Haven.Application.Common.Interfaces;
using Haven.Application.Features.Jobs.Queries.GetJobInfos;

using NSubstitute;

using Shouldly;

namespace Haven.Application.Tests.Features.Jobs.Queries.GetJobInfos;

[Category("Unit")]
public sealed class GetJobInfosHandlerTests
{
    private IJobsService _jobsService;
    private GetJobInfosHandler _sut;

    [SetUp]
    public void Setup()
    {
        _jobsService = Substitute.For<IJobsService>();
        _sut = new GetJobInfosHandler(_jobsService);
    }

    [Test]
    public async Task Handle_ShouldReturnEmptyList_WhenNoJobsExist()
    {
        _jobsService.GetJobInfosAsync(Arg.Any<CancellationToken>())
            .Returns(Result<IEnumerable<JobInfo>>.Success(Enumerable.Empty<JobInfo>()));

        var result = await _sut.Handle(new GetJobInfosQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    [Test]
    public async Task Handle_ShouldReturnJobInfos_FromService()
    {
        var jobInfo = new JobInfo
        {
            Name = "cleanup-job",
            Key = "cleanup-job",
            NextRunTime = DateTime.UtcNow.AddHours(1),
            LastRunTime = DateTime.UtcNow.AddHours(-1)
        };
        _jobsService.GetJobInfosAsync(Arg.Any<CancellationToken>())
            .Returns(Result<IEnumerable<JobInfo>>.Success(new List<JobInfo> { jobInfo }));

        var result = await _sut.Handle(new GetJobInfosQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var returned = result.Value.ShouldHaveSingleItem();
        returned.ShouldBe(jobInfo);
    }

    [Test]
    public async Task Handle_ShouldReturnFailure_WhenServiceFailsToRetrieveJobs()
    {
        _jobsService.GetJobInfosAsync(Arg.Any<CancellationToken>())
            .Returns(Result<IEnumerable<JobInfo>>.Failure(Error.Failed));

        var result = await _sut.Handle(new GetJobInfosQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe(Error.Failed);
    }
}
