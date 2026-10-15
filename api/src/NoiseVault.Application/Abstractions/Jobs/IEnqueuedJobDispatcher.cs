using NoiseVault.Domain.Enums.Jobs;

namespace NoiseVault.Application.Abstractions.Jobs
{
    public interface IEnqueuedJobDispatcher
    {
        string EnqueueBackgroundJobRun(int backgroundJobRunId, EBackgroundJobType jobType);
        string EnqueueVideoTranscode(int mediaId);
        string EnqueueBandInviteEmail(int invitationId);
        string EnqueueBandVerificationEmail(int requestId);
    }
}
