using Hangfire;
using Hangfire.Storage;

using Haven.Application.Common.Contracts;
using Haven.Application.Common.Interfaces;

namespace Haven.Infrastructure.Services;

public class JobsService : IJobsService
{
    public Task<IEnumerable<JobInfo>> GetJobInfosAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var connection = JobStorage.Current.GetConnection();
            IEnumerable<RecurringJobDto> jobDtos = connection.GetRecurringJobs();

            return Task.FromResult(jobDtos.Select(jobDto => new JobInfo
            {
                Name = jobDto.Id,
                Key = jobDto.Id,
                NextRunTime = jobDto.NextExecution,
                LastRunTime = jobDto.LastExecution
            }));
        }
        catch (Exception exception)
        {
            return Task.FromException<IEnumerable<JobInfo>>(exception);
        }
    }
}