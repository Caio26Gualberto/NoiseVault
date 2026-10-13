namespace NoiseVault.Application.Media.Dtos
{
    public class StartVideoUploadInputDto
    {
        public long SizeBytes { get; set; }
        public string? ContentType { get; set; }
    }
}
