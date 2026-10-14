using NoiseVault.Application.Common.Time;

namespace NoiseVault.Application.Archive
{
    public static class ArchiveRules
    {
        public const int TitleMinLength = 3;
        public const int TitleMaxLength = 150;
        public const int DescriptionMaxLength = 10_000;
        public const int VenueMinLength = 2;
        public const int VenueMaxLength = 120;
        public const int CaptionMaxLength = 500;
        public const int MinYear = 1950;
        public const int MaxBands = 30;
        public const int MaxMedia = 100;
        public const int AttendeesPreview = 5;
        public const int MaxPendingSubmissionsPerUser = 5;
        public const int RejectionReasonMinLength = 3;
        public const int RejectionReasonMaxLength = 500;

        public static bool IsValidDate(int year, int? month, int? day)
        {
            if (year < MinYear || year > AppTime.Today.Year)
                return false;

            if (month == null)
                return day == null;

            if (month is < 1 or > 12)
                return false;

            return day == null || (day >= 1 && day <= DateTime.DaysInMonth(year, month.Value));
        }

        public static DateTime SortDate(int year, int? month, int? day) =>
            new(year, month ?? 1, day ?? 1, 0, 0, 0, DateTimeKind.Utc);
    }
}
