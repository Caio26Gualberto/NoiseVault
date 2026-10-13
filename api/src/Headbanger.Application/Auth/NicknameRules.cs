using System.Text.RegularExpressions;

namespace Headbanger.Application.Auth
{
    public static partial class NicknameRules
    {
        public const int MinLength = 3;
        public const int MaxLength = 30;

        private static readonly HashSet<string> Reserved = new(StringComparer.Ordinal)
        {
            "admin", "headbanger", "feed", "eventos", "videos", "fotos", "bandas", "perfil", "api", "suporte",
        };

        [GeneratedRegex("^[a-z0-9](?:[a-z0-9._]{1,28}[a-z0-9])?$")]
        private static partial Regex Pattern();

        public static string Normalize(string? value) =>
            (value ?? string.Empty).Trim().ToLowerInvariant();

        public static bool IsValid(string nickname) =>
            nickname.Length >= MinLength && nickname.Length <= MaxLength && Pattern().IsMatch(nickname);

        public static bool IsReserved(string nickname) => Reserved.Contains(nickname);

        public static string DeriveFromEmail(string email)
        {
            var localPart = email.Split('@')[0].ToLowerInvariant();
            var cleaned = new string(localPart.Where(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '_').ToArray())
                .Trim('.', '_');

            if (cleaned.Length < MinLength)
                cleaned = (cleaned + "user")[..Math.Min(MaxLength, Math.Max(MinLength, cleaned.Length + 4))];

            return cleaned.Length > MaxLength ? cleaned[..MaxLength].TrimEnd('.', '_') : cleaned;
        }
    }
}
