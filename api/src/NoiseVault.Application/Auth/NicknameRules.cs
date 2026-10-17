using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace NoiseVault.Application.Auth
{
    public static partial class NicknameRules
    {
        public const int MinLength = 3;
        public const int MaxLength = 30;

        private static readonly HashSet<string> Reserved = new(StringComparer.Ordinal)
        {
            "admin", "noisevault", "feed", "eventos", "videos", "fotos", "bandas", "perfil", "api", "suporte",
        };

        [GeneratedRegex("^[a-z0-9](?:[a-z0-9._]{1,28}[a-z0-9])?$")]
        private static partial Regex Pattern();

        [GeneratedRegex("[._]{2,}")]
        private static partial Regex SeparatorRun();

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

            return Fit(cleaned);
        }

        public static string DeriveFromName(string name)
        {
            var builder = new StringBuilder();

            foreach (var c in (name ?? string.Empty).Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                    continue;

                var lower = char.ToLowerInvariant(c);

                if (char.IsAsciiLetterOrDigit(lower))
                    builder.Append(lower);
                else if (char.IsWhiteSpace(lower) || lower is '.' or '_' or '-')
                    builder.Append('.');
            }

            return Fit(SeparatorRun().Replace(builder.ToString(), ".").Trim('.', '_'));
        }

        public static string WithSuffix(string baseNickname, int suffix)
        {
            var tail = suffix.ToString(CultureInfo.InvariantCulture);
            var head = baseNickname[..Math.Min(baseNickname.Length, MaxLength - tail.Length)].TrimEnd('.', '_');
            return head + tail;
        }

        private static string Fit(string cleaned)
        {
            if (cleaned.Length < MinLength)
                cleaned = (cleaned + "user")[..Math.Min(MaxLength, Math.Max(MinLength, cleaned.Length + 4))];

            return cleaned.Length > MaxLength ? cleaned[..MaxLength].TrimEnd('.', '_') : cleaned;
        }
    }
}
