using System.Text.RegularExpressions;

namespace NoiseVault.Application.Auth
{
    public static class UserNameRules
    {
        public const int MinLength = 2;
        public const int MaxLength = 100;

        private static readonly Regex WhitespaceRun = new(@"\s+", RegexOptions.Compiled);

        public static string Normalize(string? value) =>
            WhitespaceRun.Replace((value ?? string.Empty).Trim(), " ");

        public static bool IsValid(string name) =>
            name.Length >= MinLength && name.Length <= MaxLength;
    }
}
