namespace NoiseVault.Application.Posts.Dtos
{
    public class CreatePostInputDto
    {
        public string? Content { get; set; }
        public string? BandSlug { get; set; }
        public List<int>? MediaIds { get; set; }
    }
}
