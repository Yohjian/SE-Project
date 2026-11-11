using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using QuattroLingo.Entities;
using QuattroLingo.Services;

namespace QuattroLingo.Tests.Services
{
    public class TokenServiceTests
    {
        private const string Key = "this-is-a-test-key-with-at-least-32-bytes!!";

        private static TokenService CreateService(string? expiresInMinutes = "30")
        {
            var settings = new Dictionary<string, string?>
            {
                ["Jwt:Key"] = Key,
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience"
            };
            if (expiresInMinutes is not null) settings["Jwt:ExpiresInMinutes"] = expiresInMinutes;

            return new TokenService(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());
        }

        private static ApplicationUser MakeUser(UserRole role = UserRole.User) =>
            new() { Id = "user-1", Email = "a@test.com", Role = role };

        private static JwtSecurityToken Read(string token) => new JwtSecurityTokenHandler().ReadJwtToken(token);

        private static string Claim(JwtSecurityToken jwt, string type) => jwt.Claims.First(c => c.Type == type).Value;

        // ---- GenerateAuthToken ----

        [Theory]
        [InlineData(UserRole.User, "User")]
        [InlineData(UserRole.Admin, "Admin")]
        public void GenerateAuthToken_AnyUser_ContainsSubEmailAndRoleClaims(UserRole role, string expectedRole)
        {
            var jwt = Read(CreateService().GenerateAuthToken(MakeUser(role)));

            Assert.Equal("user-1", Claim(jwt, JwtRegisteredClaimNames.Sub));
            Assert.Equal("a@test.com", Claim(jwt, JwtRegisteredClaimNames.Email));
            Assert.Equal(expectedRole, Claim(jwt, TokenService.RoleClaimType));
        }

        [Fact]
        public void GenerateAuthToken_ConfiguredIssuerAndAudience_WritesThemIntoToken()
        {
            var jwt = Read(CreateService().GenerateAuthToken(MakeUser()));

            Assert.Equal("test-issuer", jwt.Issuer);
            Assert.Equal("test-audience", Assert.Single(jwt.Audiences));
        }

        [Fact]
        public void GenerateAuthToken_ConfiguredExpiry_ExpiresAfterThatManyMinutes()
        {
            var before = DateTime.UtcNow;

            var jwt = Read(CreateService(expiresInMinutes: "30").GenerateAuthToken(MakeUser()));

            // JWT expiry has one-second precision, so allow a small window.
            Assert.InRange(jwt.ValidTo, before.AddMinutes(30).AddSeconds(-1), DateTime.UtcNow.AddMinutes(30).AddSeconds(1));
        }

        [Fact]
        public void GenerateAuthToken_NoExpiryConfigured_DefaultsToSixtyMinutes()
        {
            var before = DateTime.UtcNow;

            var jwt = Read(CreateService(expiresInMinutes: null).GenerateAuthToken(MakeUser()));

            Assert.InRange(jwt.ValidTo, before.AddMinutes(60).AddSeconds(-1), DateTime.UtcNow.AddMinutes(60).AddSeconds(1));
        }

        [Fact]
        public void GenerateAuthToken_CalledTwiceForSameUser_GivesEachTokenAUniqueJti()
        {
            var service = CreateService();

            var first = Claim(Read(service.GenerateAuthToken(MakeUser())), JwtRegisteredClaimNames.Jti);
            var second = Claim(Read(service.GenerateAuthToken(MakeUser())), JwtRegisteredClaimNames.Jti);

            Assert.NotEqual(first, second);
        }

        [Fact]
        public void GenerateAuthToken_AnyUser_IsSignedWithHmacSha256()
        {
            var jwt = Read(CreateService().GenerateAuthToken(MakeUser()));

            Assert.Equal(SecurityAlgorithms.HmacSha256, jwt.Header.Alg);
        }

        [Fact]
        public async Task GenerateAuthToken_ValidatedWithConfiguredKey_PassesValidation()
        {
            var token = CreateService().GenerateAuthToken(MakeUser());

            var result = await new JwtSecurityTokenHandler().ValidateTokenAsync(token, ValidationParameters(Key));

            Assert.True(result.IsValid);
        }

        [Fact]
        public async Task GenerateAuthToken_ValidatedWithDifferentKey_FailsValidation()
        {
            var token = CreateService().GenerateAuthToken(MakeUser());

            var result = await new JwtSecurityTokenHandler()
                .ValidateTokenAsync(token, ValidationParameters("a-completely-different-key-of-32-bytes!!"));

            Assert.False(result.IsValid);
        }

        private static TokenValidationParameters ValidationParameters(string key) => new()
        {
            ValidIssuer = "test-issuer",
            ValidAudience = "test-audience",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
        };
    }
}
