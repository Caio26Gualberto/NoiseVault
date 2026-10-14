using NoiseVault.Application.PublicProfiles.Dtos;

namespace NoiseVault.Application.Archive.Dtos
{
    public class ArchiveCommentDto
    {
        public int Id { get; set; }
        public ProfileSummaryDto Author { get; set; } = new();
        public string Text { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool CanDelete { get; set; }
    }
}
