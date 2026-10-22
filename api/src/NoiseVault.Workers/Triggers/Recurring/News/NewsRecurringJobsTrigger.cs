using Hangfire;
using NoiseVault.Application.News.Dtos;
using NoiseVault.Application.News.Interfaces;

namespace NoiseVault.Workers.Triggers.Recurring.News
{
    public class NewsRecurringJobsTrigger
    {
        private readonly INewsAppService _newsAppService;

        public NewsRecurringJobsTrigger(INewsAppService newsAppService)
        {
            _newsAppService = newsAppService;
        }

        [Queue("default")]
        [JobDisplayName("Notícias: buscar no Whiplash.Net")]
        [AutomaticRetry(Attempts = 0)]
        [DisableConcurrentExecution(timeoutInSeconds: 30)]
        public Task<NewsCollectResultDto> CollectAsync() =>
            _newsAppService.Collect();

        [Queue("default")]
        [JobDisplayName("Notícias: apagar as de mais de 30 dias")]
        [AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 60, 600 })]
        [DisableConcurrentExecution(timeoutInSeconds: 30)]
        public Task<int> PurgeAsync() =>
            _newsAppService.Purge();
    }
}
