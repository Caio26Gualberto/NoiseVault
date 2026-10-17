using Hangfire;
using NoiseVault.Application.Media.Interfaces;
using NoiseVault.Application.Posts.Interfaces;

namespace NoiseVault.Workers.Triggers.Recurring.Media
{
    public class MediaRecurringJobsTrigger
    {
        private readonly IMediaUploadAppService _mediaAppService;
        private readonly IPostAppService _postAppService;

        public MediaRecurringJobsTrigger(IMediaUploadAppService mediaAppService, IPostAppService postAppService)
        {
            _mediaAppService = mediaAppService;
            _postAppService = postAppService;
        }

        [Queue("media")]
        [JobDisplayName("Mídia: conferir conversões no MediaConvert")]
        [AutomaticRetry(Attempts = 0)]
        [DisableConcurrentExecution(timeoutInSeconds: 15)]
        public async Task PollTranscodesAsync()
        {
            foreach (var mediaId in await _mediaAppService.PollRemoteTranscodes())
                await _postAppService.PublishPendingPosts(mediaId);
        }

        [Queue("media")]
        [JobDisplayName("Mídia: limpar uploads expirados")]
        [AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 60, 600 })]
        [DisableConcurrentExecution(timeoutInSeconds: 30)]
        public async Task CleanupAsync()
        {
            await _mediaAppService.Cleanup();
        }
    }
}
