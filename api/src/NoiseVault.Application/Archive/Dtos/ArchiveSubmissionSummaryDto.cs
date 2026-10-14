namespace NoiseVault.Application.Archive.Dtos
{
    public class ArchiveSubmissionSummaryDto
    {
        public int Pending { get; set; }
        public int Published { get; set; }
        public int Rejected { get; set; }
    }
}
