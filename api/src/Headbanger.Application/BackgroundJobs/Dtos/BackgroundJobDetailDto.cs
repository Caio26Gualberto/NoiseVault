namespace Headbanger.Application.BackgroundJobs.Dtos
{
    public class BackgroundJobDetailDto : BackgroundJobListItemDto
    {
        public string? PayloadJson { get; set; }
        public string? ResultJson { get; set; }
        public string? HangfireJobId { get; set; }
    }
}
