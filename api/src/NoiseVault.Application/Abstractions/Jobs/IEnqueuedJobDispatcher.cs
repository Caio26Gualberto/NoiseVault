using NoiseVault.Domain.Enums.Jobs;

namespace NoiseVault.Application.Abstractions.Jobs
{
    public interface IEnqueuedJobDispatcher
    {
        string EnqueueBackgroundJobRun(int backgroundJobRunId, EBackgroundJobType jobType);
    }
}
