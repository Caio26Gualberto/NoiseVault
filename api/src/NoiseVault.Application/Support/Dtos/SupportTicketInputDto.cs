using NoiseVault.Domain.Enums.Support;

namespace NoiseVault.Application.Support.Dtos
{
    public class SupportTicketInputDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public List<ESupportTag>? Tags { get; set; }
        public List<ESupportArea>? Areas { get; set; }
        public List<int>? MediaIds { get; set; }
        public string? OriginPath { get; set; }
        public string? UserAgent { get; set; }
        public int? ViewportWidth { get; set; }
        public int? ViewportHeight { get; set; }
    }
}
