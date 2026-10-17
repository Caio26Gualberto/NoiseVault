using Hangfire;
using NoiseVault.Application.Common.Time;
using NoiseVault.Application.Interfaces;
using NoiseVault.Workers.Triggers.Recurring.Media;

namespace NoiseVault.Workers.Wrappers.Recurring.Media
{
    public class MediaRecurringJobsWrapper
    {
        private const string TranscodePollJobId = "media:transcode-poll";

        private readonly IRemoteVideoTranscoder _remoteTranscoder;

        public MediaRecurringJobsWrapper(IRemoteVideoTranscoder remoteTranscoder)
        {
            _remoteTranscoder = remoteTranscoder;
        }

        public void Register()
        {
            if (_remoteTranscoder.Enabled)
            {
                RecurringJob.AddOrUpdate<MediaRecurringJobsTrigger>(
                    recurringJobId: TranscodePollJobId,
                    queue: "media",
                    methodCall: trigger => trigger.PollTranscodesAsync(),
                    cronExpression: Cron.Minutely(),
                    options: new RecurringJobOptions { TimeZone = AppTime.Zone }
                );
            }
            else
            {
                RecurringJob.RemoveIfExists(TranscodePollJobId);
            }

            RecurringJob.AddOrUpdate<MediaRecurringJobsTrigger>(
                recurringJobId: "media:cleanup",
                queue: "media",
                methodCall: trigger => trigger.CleanupAsync(),
                cronExpression: Cron.Hourly(),
                options: new RecurringJobOptions { TimeZone = AppTime.Zone }
            );
        }
    }
}
