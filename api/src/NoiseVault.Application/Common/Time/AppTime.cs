namespace NoiseVault.Application.Common.Time
{
    public static class AppTime
    {
        public const string DefaultZoneId = "America/Sao_Paulo";
        private const string DefaultWindowsZoneId = "E. South America Standard Time";

        private static TimeZoneInfo _zone = ResolveZone(DefaultZoneId, DefaultWindowsZoneId);

        public static TimeZoneInfo Zone => _zone;

        public static void Configure(string? zoneId, string? windowsZoneId = null)
        {
            if (string.IsNullOrWhiteSpace(zoneId))
                return;

            _zone = ResolveZone(zoneId, windowsZoneId);
        }

        public static DateTime Now => ToLocal(DateTime.UtcNow);

        public static DateTime Today => Now.Date;

        public static DateTime TodayStartUtc => ToUtc(Today);

        public static DateTime TomorrowStartUtc => ToUtc(Today.AddDays(1));

        public static DateTime YesterdayStartUtc => ToUtc(Today.AddDays(-1));

        public static DateTime MonthStartUtc => ToUtc(StartOfMonth(Today));

        public static (DateTime FromUtc, DateTime ToUtc) MonthWindowUtc(int year, int month)
        {
            var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Unspecified);
            return (ToUtc(start), ToUtc(start.AddMonths(1)));
        }

        public static DateTime? DayStartUtc(DateTime? day) =>
            day.HasValue ? ToUtc(day.Value.Date) : null;

        public static DateTime? NextDayStartUtc(DateTime? day) =>
            day.HasValue ? ToUtc(day.Value.Date.AddDays(1)) : null;

        public static DateTime ToLocal(DateTime utc) =>
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone);

        public static DateTime ToUtc(DateTime local)
        {
            var unspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
            return new DateTimeOffset(unspecified, Zone.GetUtcOffset(unspecified)).UtcDateTime;
        }

        private static DateTime StartOfMonth(DateTime value) =>
            new(value.Year, value.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);

        private static TimeZoneInfo ResolveZone(string zoneId, string? windowsZoneId)
        {
            var candidates = string.IsNullOrWhiteSpace(windowsZoneId)
                ? new[] { zoneId }
                : new[] { zoneId, windowsZoneId };

            foreach (var id in candidates)
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(id);
                }
                catch (TimeZoneNotFoundException)
                {
                }
                catch (InvalidTimeZoneException)
                {
                }
            }

            throw new TimeZoneNotFoundException(
                $"Fuso horário da aplicação não encontrado no sistema. Tentativas: {string.Join(", ", candidates)}. Instale o tzdata na imagem.");
        }
    }
}
