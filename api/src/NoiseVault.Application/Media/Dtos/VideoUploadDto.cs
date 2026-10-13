namespace NoiseVault.Application.Media.Dtos
{
    public class VideoUploadDto
    {
        public int Id { get; set; }
        public long PartSize { get; set; }
        public List<VideoUploadPartDto> Parts { get; set; } = [];
        public DateTime ExpiresAt { get; set; }
    }
}
