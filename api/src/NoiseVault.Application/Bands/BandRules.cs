using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using NoiseVault.Application.Common.Time;
using NoiseVault.Domain.Enums.Bands;

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

        public const int MaxPendingPartnershipsPerBand = 20;
        public const int MaxPartnersPerBand = 50;

        public static readonly TimeSpan InvitationLifetime = TimeSpan.FromDays(14);
        public static readonly TimeSpan PartnershipLifetime = TimeSpan.FromDays(14);
        public static readonly TimeSpan PartnershipCooldown = TimeSpan.FromDays(30);

        private static readonly HashSet<string> ReservedSlugs = ["nova", "editar"];

        public static string NormalizeName(string? name) =>
            Spaces().Replace(name?.Trim() ?? string.Empty, " ");

        public static bool IsValidName(string name) =>
            name.Length is >= NameMinLength and <= NameMaxLength;

        public static bool IsValidFoundedYear(int? year) =>
            year is null || (year >= MinFoundedYear && year <= AppTime.Now.Year);

        public static string NormalizeSlug(string? slug) => slug?.Trim().ToLowerInvariant() ?? string.Empty;

        public static bool IsValidKind(EBandKind kind) => Enum.IsDefined(kind);

        public static string Section(EBandKind kind) => kind == EBandKind.Solo ? "artistas" : "bandas";

        public static string PagePath(EBandKind kind, string slug) => $"/{Section(kind)}/{slug}";

        public static string BaseSlug(string name, EBandKind kind)
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
                slug = $"{(kind == EBandKind.Solo ? "artista" : "banda")}-{slug}".TrimEnd('-');

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
