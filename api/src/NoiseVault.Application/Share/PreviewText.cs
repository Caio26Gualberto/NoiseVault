using System.Globalization;
using System.Text.RegularExpressions;
using NoiseVault.Application.Cities.Dtos;
using NoiseVault.Application.Common.Time;
using NoiseVault.Domain.Enums.Bands;
using NoiseVault.Domain.Enums.Profiles;

namespace NoiseVault.Application.Share
{
    public static partial class PreviewText
    {
        public static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

        private static readonly Dictionary<EMusicGenre, string> GenreNames = new()
        {
            [EMusicGenre.Heavy] = "Heavy Metal",
            [EMusicGenre.Thrash] = "Thrash",
            [EMusicGenre.Death] = "Death",
            [EMusicGenre.Black] = "Black",
            [EMusicGenre.Doom] = "Doom",
            [EMusicGenre.Power] = "Power",
            [EMusicGenre.Progressive] = "Progressivo",
            [EMusicGenre.Speed] = "Speed",
            [EMusicGenre.Groove] = "Groove",
            [EMusicGenre.NuMetal] = "Nu Metal",
            [EMusicGenre.Metalcore] = "Metalcore",
            [EMusicGenre.Deathcore] = "Deathcore",
            [EMusicGenre.Grindcore] = "Grindcore",
            [EMusicGenre.Sludge] = "Sludge",
            [EMusicGenre.Stoner] = "Stoner",
            [EMusicGenre.Folk] = "Folk Metal",
            [EMusicGenre.Symphonic] = "Sinfônico",
            [EMusicGenre.Gothic] = "Gótico",
            [EMusicGenre.Industrial] = "Industrial",
            [EMusicGenre.MelodicDeath] = "Melodic Death",
            [EMusicGenre.TechnicalDeath] = "Technical Death",
            [EMusicGenre.Blackened] = "Blackened",
            [EMusicGenre.Crossover] = "Crossover",
            [EMusicGenre.Hardcore] = "Hardcore",
            [EMusicGenre.Punk] = "Punk",
            [EMusicGenre.HardRock] = "Hard Rock",
            [EMusicGenre.ClassicRock] = "Rock Clássico",
            [EMusicGenre.Alternative] = "Alternativo",
            [EMusicGenre.Grunge] = "Grunge",
            [EMusicGenre.PostMetal] = "Post-Metal",
            [EMusicGenre.Other] = "Outros",
        };

        public static string ArchiveDate(int year, int? month, int? day)
        {
            if (month == null)
                return year.ToString(CultureInfo.InvariantCulture);

            var date = new DateTime(year, month.Value, day ?? 1);
            return day == null ? date.ToString("MMMM 'de' yyyy", PtBr) : date.ToString("d 'de' MMMM 'de' yyyy", PtBr);
        }

        public static string ArchiveIsoDate(int year, int? month, int? day) =>
            month == null ? $"{year:0000}" : day == null ? $"{year:0000}-{month:00}" : $"{year:0000}-{month:00}-{day:00}";

        public static string EventWhen(DateTime startsAtUtc) =>
            Capitalize(AppTime.ToLocal(startsAtUtc).ToString("dddd, d 'de' MMMM 'às' HH'h'mm", PtBr));

        public static string EventDay(DateTime startsAtUtc) =>
            AppTime.ToLocal(startsAtUtc).ToString("d 'de' MMMM 'de' yyyy", PtBr);

        public static string IsoLocal(DateTime utc) =>
            new DateTimeOffset(AppTime.ToLocal(utc), AppTime.Zone.GetUtcOffset(utc)).ToString("yyyy-MM-dd'T'HH:mmzzz", CultureInfo.InvariantCulture);

        public static string? City(CityDto? city, string? cityName) =>
            city != null ? $"{city.Name} - {city.Uf}" : string.IsNullOrWhiteSpace(cityName) ? null : cityName;

        public static string Genre(EMusicGenre genre) => GenreNames.GetValueOrDefault(genre, genre.ToString());

        public static string ReleaseKind(EReleaseKind kind) => kind switch
        {
            EReleaseKind.Album => "Álbum",
            EReleaseKind.Ep => "EP",
            _ => "Single",
        };

        public static string? PlainMarkdown(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            var plain = MarkdownImage().Replace(text, "$1");
            plain = MarkdownLink().Replace(plain, "$1");
            plain = MarkdownHeading().Replace(plain, string.Empty);
            plain = MarkdownQuote().Replace(plain, string.Empty);
            plain = MarkdownStrong().Replace(plain, "$2");
            plain = MarkdownEmphasis().Replace(plain, "$2");
            plain = MarkdownStrike().Replace(plain, "$1");
            return MarkdownCode().Replace(plain, "$1");
        }

        [GeneratedRegex(@"!\[([^\]]*)\]\([^)]*\)")]
        private static partial Regex MarkdownImage();

        [GeneratedRegex(@"\[([^\]]+)\]\([^)]*\)")]
        private static partial Regex MarkdownLink();

        [GeneratedRegex(@"^\s{0,3}#{1,6}\s+", RegexOptions.Multiline)]
        private static partial Regex MarkdownHeading();

        [GeneratedRegex(@"^\s{0,3}>\s?", RegexOptions.Multiline)]
        private static partial Regex MarkdownQuote();

        [GeneratedRegex(@"(\*\*|__)(.+?)\1")]
        private static partial Regex MarkdownStrong();

        [GeneratedRegex(@"(\*|_)(?=\S)(.+?)(?<=\S)\1")]
        private static partial Regex MarkdownEmphasis();

        [GeneratedRegex(@"~~(.+?)~~")]
        private static partial Regex MarkdownStrike();

        [GeneratedRegex(@"`([^`]+)`")]
        private static partial Regex MarkdownCode();

        public static string BandSentence(string name, IEnumerable<EMusicGenre> genres, string? cityName, string? uf, int? foundedYear)
        {
            var styles = genres.Where(g => g != EMusicGenre.Other).Select(g => Genre(g).ToLower(PtBr)).ToList();
            var sentence = $"{name} é uma banda";

            if (styles.Count > 0)
                sentence += $" de {NaturalList(styles)}";

            if (!string.IsNullOrWhiteSpace(cityName))
                sentence += uf != null ? $" de {cityName} ({uf})" : $" de {cityName}";

            if (foundedYear != null)
                sentence += $", formada em {foundedYear}";

            return sentence + ".";
        }

        public static string NaturalList(IReadOnlyList<string> items) => items.Count switch
        {
            0 => string.Empty,
            1 => items[0],
            _ => $"{string.Join(", ", items.Take(items.Count - 1))} e {items[^1]}",
        };

        public static string Line(params string?[] parts) => string.Join(" · ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));

        public static string? Count(int count, string singular, string plural) => count switch
        {
            <= 0 => null,
            1 => $"1 {singular}",
            _ => $"{count} {plural}",
        };

        public static string? Excerpt(string? text, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            var flat = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            return flat.Length <= maxLength ? flat : flat[..(maxLength - 1)].TrimEnd() + "…";
        }

        public static List<string> Paragraphs(string? text) =>
            string.IsNullOrWhiteSpace(text)
                ? []
                : text.Replace("\r\n", "\n").Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        public static string Capitalize(string text) => text.Length == 0 ? text : char.ToUpper(text[0], PtBr) + text[1..];
    }
}
