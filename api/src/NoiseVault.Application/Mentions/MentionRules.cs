using System.Text.RegularExpressions;

namespace NoiseVault.Application.Mentions
{
    public static partial class MentionRules
    {
        public const int MaxPerText = 10;

        [GeneratedRegex(@"(?<![a-z0-9._@-])@([a-z0-9][a-z0-9._-]{1,58}[a-z0-9])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex HandlePattern();

        public static HashSet<string> Handles(string? text)
        {
            var handles = new HashSet<string>(StringComparer.Ordinal);

            if (string.IsNullOrEmpty(text))
                return handles;

            foreach (Match match in HandlePattern().Matches(text))
                handles.Add(match.Groups[1].Value.ToLowerInvariant());

            return handles;
        }

        public static bool TooMany(string? text) => Handles(text).Count > MaxPerText;
    }
}
