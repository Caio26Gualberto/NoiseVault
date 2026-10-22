namespace NoiseVault.Application.BackgroundJobs.Interfaces
{
    public interface IRecurringJobTrigger
    {
        string? Trigger(string recurringJobId);
    }
}
