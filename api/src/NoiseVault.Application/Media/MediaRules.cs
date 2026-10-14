using NoiseVault.Application.Common.Media;
using NoiseVault.Domain.Enums.Media;

namespace NoiseVault.Application.Media
{
    public sealed record MediaProfileLimits(
        long VideoMaxBytes,
        int VideoMaxDurationSeconds,
        EImagePreset PhotoPreset,
        TimeSpan ProcessingLifetime);

    public static class MediaRules
    {
        public const int VideoDurationToleranceSeconds = 1;
        public const long PartSize = 10L * 1024 * 1024;
        public const int MaxActiveVideoUploads = 3;
        public const int ContentTypeMaxLength = 100;

        public static readonly TimeSpan PartUrlLifetime = TimeSpan.FromHours(6);
        public static readonly TimeSpan UploadLifetime = TimeSpan.FromHours(6);
        public static readonly TimeSpan UnattachedLifetime = TimeSpan.FromHours(24);

        private static readonly MediaProfileLimits PostLimits = new(
            VideoMaxBytes: 500L * 1024 * 1024,
            VideoMaxDurationSeconds: 10 * 60,
            PhotoPreset: EImagePreset.Post,
            ProcessingLifetime: TimeSpan.FromHours(2));

        private static readonly MediaProfileLimits ArchiveLimits = new(
            VideoMaxBytes: 4L * 1024 * 1024 * 1024,
            VideoMaxDurationSeconds: 2 * 60 * 60,
            PhotoPreset: EImagePreset.Archive,
            ProcessingLifetime: TimeSpan.FromHours(8));

        private static readonly MediaProfileLimits SupportLimits = new(
            VideoMaxBytes: 200L * 1024 * 1024,
            VideoMaxDurationSeconds: 2 * 60,
            PhotoPreset: EImagePreset.Post,
            ProcessingLifetime: TimeSpan.FromHours(2));

        public static MediaProfileLimits For(EMediaProfile profile) => profile switch
        {
            EMediaProfile.Post => PostLimits,
            EMediaProfile.Archive => ArchiveLimits,
            EMediaProfile.Support => SupportLimits,
            _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, "Perfil de mídia desconhecido."),
        };

        public static int PartCount(long sizeBytes) => (int)((sizeBytes + PartSize - 1) / PartSize);

        public static bool IsVideoContentType(string? contentType) =>
            !string.IsNullOrWhiteSpace(contentType)
            && contentType.Length <= ContentTypeMaxLength
            && contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);
    }
}
