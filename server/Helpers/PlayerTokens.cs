using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace QuattroLingo.Helpers
{
    public static class PlayerTokens
    {
        public static (string Token, string Hash) Generate()
        {
            var token = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
            return (token, Hash(token));
        }

        public static bool Matches(string? token, string? storedHash)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(storedHash))
            {
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(Hash(token)),
                Encoding.ASCII.GetBytes(storedHash));
        }

        private static string Hash(string token) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}