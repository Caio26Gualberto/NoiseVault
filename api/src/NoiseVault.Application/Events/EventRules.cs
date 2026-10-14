namespace NoiseVault.Application.Events
{
    public static class EventRules
    {
        public const int TitleMinLength = 3;
        public const int TitleMaxLength = 120;
        public const int DescriptionMaxLength = 5000;
        public const int VenueMinLength = 2;
        public const int VenueMaxLength = 120;
        public const int CityNameMinLength = 2;
        public const int CityNameMaxLength = 80;
        public const int TicketsUrlMaxLength = 500;
        public const decimal MaxPrice = 100_000m;
        public const int MaxCapacity = 200_000;
        public const int MaxLineup = 30;
        public const int LineupNameMaxLength = 100;
        public const int MaxActiveEventsPerUser = 20;
        public const int MaxUpcomingTake = 10;

        public static readonly TimeSpan CreationTolerance = TimeSpan.FromHours(1);
        public static readonly TimeSpan MaxDuration = TimeSpan.FromDays(7);
        public static readonly TimeSpan DefaultDuration = TimeSpan.FromHours(6);

        public static DateTime EndsAtOrDefault(DateTime startsAt, DateTime? endsAt) => endsAt ?? startsAt.Add(DefaultDuration);

        public static bool IsValidTicketsUrl(string? url) =>
            url is null
            || (url.Length <= TicketsUrlMaxLength
                && Uri.TryCreate(url, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp));
    }
}
