namespace NoiseVault.Application.BandOpenings
{
    public static class BandOpeningRules
    {
        public const int DescriptionMaxLength = 500;
        public const int InterestMessageMaxLength = 300;
        public const int MaxOpenPerBand = 5;
        public static readonly TimeSpan Lifetime = TimeSpan.FromDays(60);
    }
}
