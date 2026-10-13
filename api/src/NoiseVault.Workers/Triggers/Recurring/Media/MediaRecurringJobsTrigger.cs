using Hangfire;
using NoiseVault.Application.Media.Interfaces;

namespace NoiseVault.Workers.Triggers.Recurring.Media
{
    public class MediaRecurringJobsTrigger
    {
        private readonly IMediaUploadAppService _mediaAppService;

        public MediaRecurringJobsTrigger(IMediaUploadAppService mediaAppService)
        {
            _mediaAppService = mediaAppService;
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
