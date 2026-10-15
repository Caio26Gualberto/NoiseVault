using Hangfire;
using NoiseVault.Application.Abstractions.Jobs;
using NoiseVault.Domain.Enums.Jobs;
using NoiseVault.Workers.Triggers.Enqueued.Media;
using NoiseVault.Workers.Triggers.Enqueued.Notifications;

namespace NoiseVault.Workers.Dispatch
{
    public class EnqueuedJobDispatcher : IEnqueuedJobDispatcher
    {
        public string EnqueueBackgroundJobRun(int backgroundJobRunId, EBackgroundJobType jobType) =>
            jobType switch
            {
                _ => throw new InvalidOperationException(
                    $"Nenhuma fila definida para o job {jobType}. Mapeie o tipo em {nameof(EnqueuedJobDispatcher)}."),
            };

        public string EnqueueVideoTranscode(int mediaId) =>
            BackgroundJob.Enqueue<MediaEnqueuedJobsTrigger>(t => t.TranscodeVideoAsync(mediaId, null));

        public string EnqueueBandInviteEmail(int invitationId) =>
            BackgroundJob.Enqueue<NotificationsEnqueuedJobsTrigger>(t => t.SendBandInviteEmailAsync(invitationId));

        public string EnqueueBandVerificationEmail(int requestId) =>
            BackgroundJob.Enqueue<NotificationsEnqueuedJobsTrigger>(t => t.SendBandVerificationEmailAsync(requestId));
    }
}
