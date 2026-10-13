namespace NoiseVault.Domain.Entities
{
    public class PostMedia : EntityBase
    {
        public int PostId { get; set; }
        public int MediaUploadId { get; set; }
        public int Order { get; set; }

        public Post? Post { get; set; }
        public MediaUpload? MediaUpload { get; set; }
    }
}
