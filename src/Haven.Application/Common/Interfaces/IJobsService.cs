using Haven.Application.Common.Contracts;

namespace Haven.Application.Common.Interfaces;

public interface IJobsService
{
    Task<IEnumerable<JobInfo>> GetJobInfosAsync(CancellationToken cancellationToken);
    
}