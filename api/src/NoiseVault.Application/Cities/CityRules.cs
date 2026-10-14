using System.Globalization;
using System.Text;

namespace NoiseVault.Application.Cities
{
    public static class CityRules
    {
        public const int CityNameMinLength = 2;
        public const int CityNameMaxLength = 80;
        public const int SearchMinLength = 2;

        public static readonly int[] AbcCityIds = [3547809, 3548708, 3548807, 3513801, 3529401, 3543303, 3544103];

        public static string Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(decomposed.Length);
            var lastWasSpace = false;

            foreach (var character in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                    continue;

                var isSpace = char.IsWhiteSpace(character);

                if (isSpace && lastWasSpace)
                    continue;

                builder.Append(isSpace ? ' ' : character);
                lastWasSpace = isSpace;
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
