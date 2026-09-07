using Hangfire;
using Hangfire.Storage;

using Haven.Application.Common;
using Haven.Application.Common.Contracts;
using Haven.Application.Common.Interfaces;

namespace Haven.Infrastructure.Services;

public class JobsService : IJobsService
{
    public Task<Result<IEnumerable<JobInfo>>> GetJobInfosAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var connection = JobStorage.Current.GetConnection();
            IEnumerable<RecurringJobDto> jobDtos = connection.GetRecurringJobs();

            IEnumerable<JobInfo> jobInfos = jobDtos.Select(jobDto => new JobInfo
            {
                Name = jobDto.Id,
                Key = jobDto.Id,
                NextRunTime = jobDto.NextExecution,
                LastRunTime = jobDto.LastExecution
            });

            return Task.FromResult(Result<IEnumerable<JobInfo>>.Success(jobInfos));
        }
        catch
        {
            return Task.FromResult(Result<IEnumerable<JobInfo>>.Failure(Error.Failed));
        }
    }

    public Task<Result> TriggerJobAsync(string jobKey, CancellationToken cancellationToken)
    {
        try
        {
            RecurringJob.TriggerJob(jobKey);
            return Task.FromResult(Result.Success());
        }
        catch
        {
            return Task.FromResult(Result.Failure(Error.Failed));
        }
    }
}