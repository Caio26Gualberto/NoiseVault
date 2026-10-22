using Hangfire;
using NoiseVault.Application.Bands.Interfaces;

namespace NoiseVault.Workers.Triggers.Recurring.Discography
{
    public class DiscographyRecurringJobsTrigger
    {
        private readonly IBandDiscographyAppService _discographyAppService;

        public DiscographyRecurringJobsTrigger(IBandDiscographyAppService discographyAppService)
        {
            _discographyAppService = discographyAppService;
        }

        [Queue("default")]
        [JobDisplayName("Discografia: atualizar do Spotify")]
        [AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 60, 600 })]
        [DisableConcurrentExecution(timeoutInSeconds: 60)]
        public Task<int> RefreshAsync() =>
            _discographyAppService.RefreshStale();
    }
}
