using Headbanger.Domain.Enums.Jobs;

namespace Headbanger.Application.Abstractions.Jobs
{
    public interface IEnqueuedJobDispatcher
    {
        string EnqueueBackgroundJobRun(int backgroundJobRunId, EBackgroundJobType jobType);
    }
}
