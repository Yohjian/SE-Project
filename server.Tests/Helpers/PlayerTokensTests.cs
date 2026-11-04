using QuattroLingo.Helpers;

namespace QuattroLingo.Tests.Helpers
{
    public class PlayerTokensTests
    {
        [Fact]
        public void Generate_TokenMatchesItsOwnHash()
        {
            var (token, hash) = PlayerTokens.Generate();

            Assert.True(PlayerTokens.Matches(token, hash));
        }

        [Fact]
        public void Generate_ReturnsDifferentTokensEachTime()
        {
            Assert.NotEqual(PlayerTokens.Generate().Token, PlayerTokens.Generate().Token);
        }

        [Fact]
        public void Matches_WrongToken_ReturnsFalse()
        {
            var (_, hash) = PlayerTokens.Generate();
            var (otherToken, _) = PlayerTokens.Generate();

            Assert.False(PlayerTokens.Matches(otherToken, hash));
        }

        [Theory]
        [InlineData(null, "abc")]
        [InlineData("", "abc")]
        [InlineData("abc", null)]
        [InlineData("abc", "")]   // legacy participants have an empty hash and must never match
        public void Matches_MissingValues_ReturnsFalse(string? token, string? hash)
        {
            Assert.False(PlayerTokens.Matches(token, hash));
        }
    }
}