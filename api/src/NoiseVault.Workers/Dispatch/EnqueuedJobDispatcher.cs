using NoiseVault.Application.Abstractions.Jobs;
using NoiseVault.Domain.Enums.Jobs;

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
    }
}
