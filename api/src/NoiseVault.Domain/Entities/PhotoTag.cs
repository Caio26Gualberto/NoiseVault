namespace NoiseVault.Domain.Entities
{
    public class PhotoTag : EntityBase
    {
        public int MediaUploadId { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public int? TaggedUserId { get; set; }
        public string? Name { get; set; }
        public int TaggedByUserId { get; set; }

        public MediaUpload? MediaUpload { get; set; }
        public User? TaggedUser { get; set; }
        public User? TaggedBy { get; set; }
    }
}
