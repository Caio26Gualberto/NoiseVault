namespace NoiseVault.Application.Bands.Dtos
{
    public class BandVerificationSummaryDto
    {
        public int Pending { get; set; }
        public int Approved { get; set; }
        public int Rejected { get; set; }
    }
}
