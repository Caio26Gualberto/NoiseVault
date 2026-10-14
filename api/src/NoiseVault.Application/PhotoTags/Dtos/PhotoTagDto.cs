using NoiseVault.Application.PublicProfiles.Dtos;

namespace NoiseVault.Application.PhotoTags.Dtos
{
    public class PhotoTagDto
    {
        public int Id { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public ProfileSummaryDto? User { get; set; }
        public string? Name { get; set; }
        public bool CanRemove { get; set; }
    }
}
