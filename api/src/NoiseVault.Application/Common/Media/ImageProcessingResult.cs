namespace NoiseVault.Application.Common.Media
{
    public enum EImageRejection
    {
        None = 0,
        Unsupported = 1,
        TooSmall = 2,
    }

    public sealed record ProcessedImage(byte[] Content, int Width, int Height, string ContentType, string Extension);

    public sealed record ImageProcessingResult(ProcessedImage? Image, EImageRejection Rejection)
    {
        public bool Succeeded => Image is not null;

        public static ImageProcessingResult Ok(ProcessedImage image) => new(image, EImageRejection.None);

        public static ImageProcessingResult Reject(EImageRejection rejection) => new(null, rejection);
    }
}
