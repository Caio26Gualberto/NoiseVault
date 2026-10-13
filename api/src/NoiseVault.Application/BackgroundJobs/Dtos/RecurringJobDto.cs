namespace NoiseVault.Application.BackgroundJobs.Dtos
{
    public class RecurringJobDto
    {
        public string Id { get; set; } = string.Empty;
        public string? Cron { get; set; }
        public string? Queue { get; set; }
        public DateTime? LastExecution { get; set; }
        public DateTime? NextExecution { get; set; }
        public string? LastJobState { get; set; }
    }
}
