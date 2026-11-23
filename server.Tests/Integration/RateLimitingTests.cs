using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace QuattroLingo.Tests.Integration
{
    public class RateLimitingTests(QuattroLingoFactory factory) : IClassFixture<QuattroLingoFactory>
    {
        private HttpClient LimitedClient(Dictionary<string, string?> settings) =>
            factory.WithWebHostBuilder(b => b.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(settings))).CreateClient();

        [Fact]
        public async Task Login_AboveLimit_Returns429WithRetryAfterAndProblemJson()
        {
            var client = LimitedClient(new()
            {
                ["RateLimiting:Enabled"] = "true",
                ["RateLimiting:AuthPerMinute"] = "3"
            });

            for (var i = 0; i < 3; i++)
            {
                var ok = await client.PostAsJsonAsync("/api/auth/login",
                    new { email = "nobody@test.com", password = "Password1" });
                Assert.NotEqual(HttpStatusCode.TooManyRequests, ok.StatusCode);
            }

            var limited = await client.PostAsJsonAsync("/api/auth/login",
                new { email = "nobody@test.com", password = "Password1" });

            Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
            Assert.True(limited.Headers.Contains("Retry-After"));
            Assert.Equal("application/problem+json", limited.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task RoomLookup_AboveLimit_Returns429ButOtherRoutesStillWork()
        {
            var client = LimitedClient(new()
            {
                ["RateLimiting:Enabled"] = "true",
                ["RateLimiting:RoomLookupPerMinute"] = "2"
            });

            await client.GetAsync("/api/rooms/123456");
            await client.GetAsync("/api/rooms/123456");
            var limited = await client.GetAsync("/api/rooms/123456");
            var other = await client.GetAsync("/api/rooms/public");

            Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
            Assert.NotEqual(HttpStatusCode.TooManyRequests, other.StatusCode);
        }

        [Fact]
        public async Task Disabled_NeverLimits()
        {
            var client = LimitedClient(new()
            {
                ["RateLimiting:Enabled"] = "false",
                ["RateLimiting:AuthPerMinute"] = "1"
            });

            for (var i = 0; i < 5; i++)
            {
                var r = await client.PostAsJsonAsync("/api/auth/login",
                    new { email = "nobody@test.com", password = "Password1" });
                Assert.NotEqual(HttpStatusCode.TooManyRequests, r.StatusCode);
            }
        }
    }
}
