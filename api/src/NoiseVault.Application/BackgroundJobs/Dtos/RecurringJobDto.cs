namespace NoiseVault.Application.BackgroundJobs.Dtos
{
    public class RecurringJobDto
    {
        public string Id { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? Cron { get; set; }
        public string? Queue { get; set; }
        public DateTime? LastExecution { get; set; }
        public DateTime? NextExecution { get; set; }
        public string? LastJobId { get; set; }
        public string? LastJobState { get; set; }
        public long? LastDurationMs { get; set; }
        public string? LastResult { get; set; }
        public string? LastError { get; set; }
        public string? LastErrorDetails { get; set; }
        public string? Error { get; set; }
        public bool CanTrigger { get; set; }
    }
}
