namespace NoiseVault.Application.BackgroundJobs.Dtos
{
    public class FailedJobDto
    {
        public string JobId { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? RecurringJobId { get; set; }
        public string? Queue { get; set; }
        public DateTime? FailedAt { get; set; }
        public string? ExceptionType { get; set; }
        public string? ExceptionMessage { get; set; }
        public string? ExceptionDetails { get; set; }
    }
}
