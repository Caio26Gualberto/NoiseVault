using NoiseVault.Application.BackgroundJobs.Dtos;

namespace NoiseVault.Application.BackgroundJobs.Interfaces
{
    public interface IRecurringJobReader
    {
        IReadOnlyList<RecurringJobDto> GetRecurringJobs();
    }
}
