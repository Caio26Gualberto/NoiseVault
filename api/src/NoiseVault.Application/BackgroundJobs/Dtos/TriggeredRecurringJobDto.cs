namespace NoiseVault.Application.BackgroundJobs.Dtos
{
    public class TriggeredRecurringJobDto
    {
        public string RecurringJobId { get; set; } = string.Empty;
        public string JobId { get; set; } = string.Empty;
    }
}
