using NoiseVault.Application.PhotoTags.Dtos;
using NoiseVault.Domain.Enums.Media;

namespace NoiseVault.Application.Media.Dtos
{
    public class MediaDto
    {
        public int Id { get; set; }
        public EMediaKind Kind { get; set; }
        public EMediaStatus Status { get; set; }
        public string? Url { get; set; }
        public string? PosterUrl { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public int? DurationSeconds { get; set; }
        public List<PhotoTagDto> Tags { get; set; } = [];
    }
}
