using Haven.Application.Common.Contracts;

namespace Haven.Application.Common.Interfaces;

public interface IJobsService
{
    Task<Result<IEnumerable<JobInfo>>> GetJobInfosAsync(CancellationToken cancellationToken);
    Task<Result> TriggerJobAsync(string jobKey, CancellationToken cancellationToken);
}