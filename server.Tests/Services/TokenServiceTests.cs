using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using QuattroLingo.Entities;
using QuattroLingo.Services;

namespace QuattroLingo.Tests.Services;

public class TokenServiceTests
{
    private static TokenService CreateService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "this-is-a-test-key-with-at-least-32-bytes!!",
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Jwt:ExpiresInMinutes"] = "30"
            })
            .Build();

        return new TokenService(config);
    }

    [Theory]
    [InlineData(UserRole.User, "User")]
    [InlineData(UserRole.Admin, "Admin")]
    public void GenerateAuthToken_ContainsSubEmailAndRoleClaims(UserRole role, string expectedRole)
    {
        var user = new ApplicationUser { Id = "user-1", Email = "a@test.com", Role = role };

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(CreateService().GenerateAuthToken(user));

        Assert.Equal("user-1", jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal("a@test.com", jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal(expectedRole, jwt.Claims.First(c => c.Type == TokenService.RoleClaimType).Value);
        Assert.Equal("test-issuer", jwt.Issuer);
    }
}