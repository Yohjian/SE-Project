using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuattroLingo.Data;

namespace QuattroLingo.Tests.Integration
{
    public class AuthorizationEndToEndTests(QuattroLingoFactory factory) : IClassFixture<QuattroLingoFactory>
    {
        private const string Password = "Password1";

        private record LoginResult(string Token, string Email, string Role);
        private record SetResult(int Id, string Name, int CardCount);
        private record FolderResult(int Id, string Name);

        private static async Task<int> CreateFolderAsync(HttpClient client)
        {
            var response = await client.PostAsJsonAsync("/api/set-folders", new { name = "Folder" });
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<FolderResult>())!.Id;
        }

        private static string NewEmail() => $"{Guid.NewGuid():N}@test.com";

        private async Task RegisterAsync(HttpClient client, string email)
        {
            var response = await client.PostAsJsonAsync("/api/auth/register", new { email, password = Password });
            response.EnsureSuccessStatusCode();
        }

        private async Task<HttpClient> LoggedInClientAsync(string? email = null)
        {
            email ??= NewEmail();
            var client = factory.CreateClient();
            await RegisterAsync(client, email);

            var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
            login.EnsureSuccessStatusCode();
            var body = await login.Content.ReadFromJsonAsync<LoginResult>();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
            return client;
        }

        private static void AssertProblemJson(HttpResponseMessage response) =>
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);



        [Fact]
        public async Task ProtectedEndpoint_WithoutToken_Returns401()
        {
            var response = await factory.CreateClient().GetAsync("/api/sets");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ProtectedEndpoint_WithGarbageToken_Returns401()
        {
            var client = factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-real-token");

            var response = await client.GetAsync("/api/sets");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ProtectedEndpoint_WithValidToken_Returns200()
        {
            var client = await LoggedInClientAsync();

            var response = await client.GetAsync("/api/sets");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }



        [Fact]
        public async Task Login_ReturnsTokenAndUserRole()
        {
            var email = NewEmail();
            var client = factory.CreateClient();
            await RegisterAsync(client, email);

            var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<LoginResult>();
            Assert.False(string.IsNullOrEmpty(body!.Token));
            Assert.Equal("User", body.Role);
        }

        [Fact]
        public async Task Register_DuplicateEmail_Returns400ProblemDetails()
        {
            var email = NewEmail();
            var client = factory.CreateClient();
            await RegisterAsync(client, email);

            var response = await client.PostAsJsonAsync("/api/auth/register", new { email, password = Password });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            AssertProblemJson(response);
        }

        [Fact]
        public async Task Login_WrongPassword_Returns401ProblemDetails()
        {
            var email = NewEmail();
            var client = factory.CreateClient();
            await RegisterAsync(client, email);

            var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "WrongPassword1" });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            AssertProblemJson(response);
        }

        [Fact]
        public async Task Login_DeactivatedUser_Returns403()
        {
            var email = NewEmail();
            var client = factory.CreateClient();
            await RegisterAsync(client, email);

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var user = await db.Users.SingleAsync(u => u.Email == email);
                user.IsActive = false;
                await db.SaveChangesAsync();
            }

            var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            AssertProblemJson(response);
        }



        [Fact]
        public async Task Sets_OwnerCanCreateAndListASet()
        {
            var client = await LoggedInClientAsync();

            var folderId = await CreateFolderAsync(client);

            var create = await client.PostAsJsonAsync("/api/sets", new { name = "Animals", folderId });
            Assert.Equal(HttpStatusCode.OK, create.StatusCode);

            var sets = await client.GetFromJsonAsync<List<SetResult>>("/api/sets");
            var set = Assert.Single(sets!);
            Assert.Equal("Animals", set.Name);
        }

        [Fact]
        public async Task Sets_AnotherUsersSet_Returns404ProblemDetails()
        {
            var owner = await LoggedInClientAsync();
            var folderId = await CreateFolderAsync(owner);
            var create = await owner.PostAsJsonAsync("/api/sets", new { name = "Private", folderId });
            var created = await create.Content.ReadFromJsonAsync<SetResult>();

            var intruder = await LoggedInClientAsync();
            var response = await intruder.GetAsync($"/api/sets/{created!.Id}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            AssertProblemJson(response);
        }

        [Fact]
        public async Task Quizzes_NumberTooBigForInt_Returns400WithReadableMessage()
        {
            var client = await LoggedInClientAsync();
            var json = """
                {"title":"t","description":null,"questions":[{"text":"Q?","timeLimitSeconds":30,
                "points":99999999999,"orderIndex":0,
                "answerOptions":[{"text":"a","isCorrect":true},{"text":"b","isCorrect":false}]}]}
                """;

            var response = await client.PostAsync("/api/quizzes",
                new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            AssertProblemJson(response);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("points", body);
            Assert.DoesNotContain("could not be converted", body);
            Assert.DoesNotContain("request field is required", body);
        }
    }
}
