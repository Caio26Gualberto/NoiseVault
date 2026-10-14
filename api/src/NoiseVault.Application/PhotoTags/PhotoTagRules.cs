namespace NoiseVault.Application.PhotoTags
{
    public static class PhotoTagRules
    {
        public const int MaxPerPhoto = 30;
        public const int NameMinLength = 2;
        public const int NameMaxLength = 80;

        public static bool IsValidPosition(double value) => !double.IsNaN(value) && value is >= 0 and <= 1;
    }
}
