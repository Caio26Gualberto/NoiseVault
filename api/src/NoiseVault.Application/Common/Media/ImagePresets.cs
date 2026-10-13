namespace NoiseVault.Application.Common.Media
{
    public static class ImagePresets
    {
        public const long AvatarMaxBytes = 5 * 1024 * 1024;
        public const long BannerMaxBytes = 10 * 1024 * 1024;

        public static long MaxBytes(EImagePreset preset) => preset switch
        {
            EImagePreset.Avatar => AvatarMaxBytes,
            EImagePreset.Banner => BannerMaxBytes,
            _ => throw new ArgumentOutOfRangeException(nameof(preset), preset, "Preset de imagem desconhecido."),
        };
    }
}
