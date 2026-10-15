namespace NoiseVault.Application.Common.Media
{
    public static class ImagePresets
    {
        public const long AvatarMaxBytes = 5 * 1024 * 1024;
        public const long BannerMaxBytes = 10 * 1024 * 1024;
        public const long PostMaxBytes = 15 * 1024 * 1024;
        public const long ArchiveMaxBytes = 40 * 1024 * 1024;
        public const long FlyerMaxBytes = 10 * 1024 * 1024;

        public static long MaxBytes(EImagePreset preset) => preset switch
        {
            EImagePreset.Avatar => AvatarMaxBytes,
            EImagePreset.Banner => BannerMaxBytes,
            EImagePreset.Post => PostMaxBytes,
            EImagePreset.Archive => ArchiveMaxBytes,
            EImagePreset.Flyer => FlyerMaxBytes,
            _ => throw new ArgumentOutOfRangeException(nameof(preset), preset, "Preset de imagem desconhecido."),
        };
    }
}
