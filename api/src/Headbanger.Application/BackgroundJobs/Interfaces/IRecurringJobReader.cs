using Headbanger.Application.BackgroundJobs.Dtos;

namespace Headbanger.Application.BackgroundJobs.Interfaces
{
    public interface IRecurringJobReader
    {
        IReadOnlyList<RecurringJobDto> GetRecurringJobs();
    }
}
