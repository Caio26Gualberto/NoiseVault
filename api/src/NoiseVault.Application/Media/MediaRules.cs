namespace NoiseVault.Application.Media
{
    public static class MediaRules
    {
        public const long VideoMaxBytes = 500L * 1024 * 1024;
        public const int VideoMaxDurationSeconds = 600;
        public const int VideoDurationToleranceSeconds = 1;
        public const long PartSize = 10L * 1024 * 1024;
        public const int MaxActiveVideoUploads = 3;
        public const int ContentTypeMaxLength = 100;

        public static readonly TimeSpan PartUrlLifetime = TimeSpan.FromHours(6);
        public static readonly TimeSpan UploadLifetime = TimeSpan.FromHours(6);
        public static readonly TimeSpan ProcessingLifetime = TimeSpan.FromHours(2);
        public static readonly TimeSpan UnattachedLifetime = TimeSpan.FromHours(24);

        public static int PartCount(long sizeBytes) => (int)((sizeBytes + PartSize - 1) / PartSize);

        public static bool IsVideoContentType(string? contentType) =>
            !string.IsNullOrWhiteSpace(contentType)
            && contentType.Length <= ContentTypeMaxLength
            && contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);
    }
}
