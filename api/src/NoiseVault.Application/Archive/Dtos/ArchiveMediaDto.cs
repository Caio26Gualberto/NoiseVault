using NoiseVault.Application.Media.Dtos;

namespace NoiseVault.Application.Archive.Dtos
{
    public class ArchiveMediaDto
    {
        public MediaDto Media { get; set; } = new();
        public string? Caption { get; set; }
    }
}
