using Hangfire;

using Haven.Application.Common.Interfaces;

namespace Haven.Infrastructure.BackgroundJobs;

public sealed class HangfireConfigurationWriteScheduler(IBackgroundJobClient backgroundJobClient)
    : IConfigurationWriteScheduler
{
    public void ScheduleWrite()
        => backgroundJobClient.Enqueue<ConfigurationWriteBackgroundJob>(job => job.ExecuteAsync());
}