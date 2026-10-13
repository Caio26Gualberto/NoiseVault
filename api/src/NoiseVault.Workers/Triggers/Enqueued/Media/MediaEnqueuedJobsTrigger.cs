using Hangfire;
using Hangfire.Server;
using NoiseVault.Application.Posts.Interfaces;

namespace NoiseVault.Workers.Triggers.Enqueued.Media
{
    public class MediaEnqueuedJobsTrigger
    {
        public const int TranscodeAttempts = 2;

        private readonly IPostAppService _postAppService;

        public MediaEnqueuedJobsTrigger(IPostAppService postAppService)
        {
            _postAppService = postAppService;
        }

        [Queue("media")]
        [JobDisplayName("Mídia: converter vídeo {0}")]
        [AutomaticRetry(Attempts = TranscodeAttempts, DelaysInSeconds = new[] { 60, 300 }, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
        public Task TranscodeVideoAsync(int mediaUploadId, PerformContext? context)
        {
            var retryCount = context?.GetJobParameter<int>("RetryCount") ?? 0;
            return _postAppService.ProcessVideo(mediaUploadId, finalAttempt: retryCount >= TranscodeAttempts);
        }
    }
}
