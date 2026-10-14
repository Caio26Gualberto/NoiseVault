using System.Globalization;
using NoiseVault.Domain.Enums.Support;

namespace NoiseVault.Application.Support
{
    public static class SupportRules
    {
        public const int TitleMinLength = 5;
        public const int TitleMaxLength = 120;
        public const int DescriptionMinLength = 20;
        public const int DescriptionMaxLength = 5000;
        public const int MessageMaxLength = 2000;
        public const int NoteMaxLength = 5000;
        public const int OriginPathMaxLength = 300;
        public const int UserAgentMaxLength = 500;
        public const int ViewportMax = 20000;
        public const int MaxMedia = 6;
        public const int MaxVideos = 1;
        public const int MaxOpenTicketsPerUser = 10;

        private const string ProtocolPrefix = "NV-";

        public static string Protocol(int ticketId) =>
            ProtocolPrefix + ticketId.ToString("D4", CultureInfo.InvariantCulture);

        public static int? ParseProtocol(string? search)
        {
            var text = search?.Trim();

            if (string.IsNullOrEmpty(text))
                return null;

            if (text.StartsWith(ProtocolPrefix, StringComparison.OrdinalIgnoreCase))
                text = text[ProtocolPrefix.Length..];

            return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var id) && id > 0 ? id : null;
        }

        public static bool IsOpen(ESupportTicketStatus status) =>
            status is ESupportTicketStatus.Open or ESupportTicketStatus.InProgress;

        public static bool IsClosed(ESupportTicketStatus status) => !IsOpen(status);

        public static string? Truncate(string? value, int maxLength)
        {
            var trimmed = value?.Trim();

            if (string.IsNullOrEmpty(trimmed))
                return null;

            return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
        }

        public static string? NormalizeOriginPath(string? value)
        {
            var path = value?.Trim();

            if (string.IsNullOrEmpty(path) || !path.StartsWith('/'))
                return null;

            var cut = path.IndexOfAny(['?', '#']);

            if (cut >= 0)
                path = path[..cut];

            return Truncate(path, OriginPathMaxLength);
        }

        public static int? NormalizeViewport(int? value) =>
            value is > 0 and <= ViewportMax ? value : null;
    }
}
