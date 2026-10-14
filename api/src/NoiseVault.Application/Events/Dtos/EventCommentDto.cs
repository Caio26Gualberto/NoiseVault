using NoiseVault.Application.Mentions.Dtos;
using NoiseVault.Application.PublicProfiles.Dtos;

namespace NoiseVault.Application.Events.Dtos
{
    public class EventCommentDto
    {
        public int Id { get; set; }
        public ProfileSummaryDto Author { get; set; } = new();
        public string Text { get; set; } = string.Empty;
        public List<MentionDto> Mentions { get; set; } = [];
        public DateTime CreatedAt { get; set; }
        public bool CanDelete { get; set; }
    }
}
