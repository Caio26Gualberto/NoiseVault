using System.Security.Cryptography;

namespace Headbanger.Application.Users
{
    public static class TemporaryPasswordGenerator
    {
        private const string Letters = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz";
        private const string Digits = "23456789";
        private const int Length = 12;
        private const int MinDigits = 3;

        public static string Generate()
        {
            var chars = new char[Length];

            for (var i = 0; i < MinDigits; i++)
                chars[i] = Digits[RandomNumberGenerator.GetInt32(Digits.Length)];

            for (var i = MinDigits; i < Length; i++)
                chars[i] = Letters[RandomNumberGenerator.GetInt32(Letters.Length)];

            for (var i = chars.Length - 1; i > 0; i--)
            {
                var j = RandomNumberGenerator.GetInt32(i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }

            return new string(chars);
        }
    }
}
