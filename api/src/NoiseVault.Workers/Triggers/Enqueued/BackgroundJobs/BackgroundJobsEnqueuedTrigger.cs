using NoiseVault.Application.BackgroundJobs.Interfaces;

namespace NoiseVault.Workers.Triggers.Enqueued.BackgroundJobs
{
    public class BackgroundJobsEnqueuedTrigger
    {
        private readonly IBackgroundJobAppService _appService;

        public BackgroundJobsEnqueuedTrigger(IBackgroundJobAppService appService)
        {
            _appService = appService;
        }

        public Task RunAsync(int backgroundJobRunId) => _appService.ExecuteAsync(backgroundJobRunId);
    }
}
