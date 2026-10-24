using NoiseVault.Domain.Enums.Bands;

namespace NoiseVault.Application.Bands
{
    public static class BandVerificationRules
    {
        public const int MessageMinLength = 20;
        public const int MessageMaxLength = 1000;
        public const int ContactPhoneMaxLength = 30;
        public const int RejectionReasonMinLength = 3;
        public const int RejectionReasonMaxLength = 500;
        public const int MinBandMembers = 2;
        public const int MinSoloMembers = 1;

        public static int MinMembers(EBandKind kind) => kind == EBandKind.Solo ? MinSoloMembers : MinBandMembers;
    }
}
