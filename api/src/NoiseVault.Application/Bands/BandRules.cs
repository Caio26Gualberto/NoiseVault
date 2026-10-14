using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using NoiseVault.Application.Common.Time;

namespace NoiseVault.Application.Bands
{
    public static partial class BandRules
    {
        public const int NameMinLength = 2;
        public const int NameMaxLength = 100;
        public const int DescriptionMaxLength = 1000;
        public const int SlugMinLength = 3;
        public const int SlugMaxLength = 60;
        public const int MinFoundedYear = 1950;
        public const int MaxBandsPerUser = 10;
        public const int MaxPendingInvitationsPerBand = 20;
        public const int SuggestedCount = 5;
        public const int MinSearchLength = 2;

        public static readonly TimeSpan InvitationLifetime = TimeSpan.FromDays(14);

        private static readonly HashSet<string> ReservedSlugs = ["nova", "editar"];

        public static string NormalizeName(string? name) =>
            Spaces().Replace(name?.Trim() ?? string.Empty, " ");

        public static bool IsValidName(string name) =>
            name.Length is >= NameMinLength and <= NameMaxLength;

        public static bool IsValidFoundedYear(int? year) =>
            year is null || (year >= MinFoundedYear && year <= AppTime.Now.Year);

        public static string NormalizeSlug(string? slug) => slug?.Trim().ToLowerInvariant() ?? string.Empty;

        public static string BaseSlug(string name)
        {
            var decomposed = name.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(decomposed.Length);

            foreach (var c in decomposed)
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    builder.Append(c);

            var slug = NonAlphanumeric().Replace(builder.ToString().ToLowerInvariant(), "-").Trim('-');

            if (slug.Length > SlugMaxLength)
                slug = slug[..SlugMaxLength].TrimEnd('-');

            if (slug.Length < SlugMinLength || ReservedSlugs.Contains(slug))
                slug = $"banda-{slug}".TrimEnd('-');

            return slug;
        }

        public static string WithSuffix(string baseSlug, int suffix)
        {
            var tail = $"-{suffix}";
            var head = baseSlug.Length + tail.Length > SlugMaxLength
                ? baseSlug[..(SlugMaxLength - tail.Length)].TrimEnd('-')
                : baseSlug;

            return head + tail;
        }

        [GeneratedRegex(@"\s+")]
        private static partial Regex Spaces();

        [GeneratedRegex("[^a-z0-9]+")]
        private static partial Regex NonAlphanumeric();
    }
}
