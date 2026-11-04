using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using QuattroLingo.Data;
using QuattroLingo.Entities;
using QuattroLingo.Enums;
using QuattroLingo.Helpers;
using QuattroLingo.Services;

namespace QuattroLingo.Tests.Integration
{
    public class GameHubTests
    {
        private const string JoinCode = "123456";
        private const string HostId = "host-1";

        // Same values as the static constructor of QuattroLingoFactory.
        private const string JwtKey = "end-to-end-test-signing-key-at-least-32-bytes!";
        private const string JwtIssuer = "quattrolingo-tests";

        /// <summary>Real timers, just short ones, so a whole game takes well under a second.</summary>
        private sealed class FastGameFactory : QuattroLingoFactory
        {
            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                base.ConfigureWebHost(builder);

                builder.ConfigureServices(services => services.AddSingleton(new GameOptions
                {
                    CountdownDelay = TimeSpan.FromMilliseconds(50),
                    ReviewDelay = TimeSpan.FromMilliseconds(50)
                }));
            }
        }

        private sealed record Seed(string PlayerToken, int CorrectOptionId);

        private static async Task<Seed> SeedAsync(IServiceProvider services)
        {
            var (token, hash) = PlayerTokens.Generate();

            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var quiz = new Quiz
            {
                OwnerId = HostId,
                Title = "Smoke quiz",
                Questions =
                [
                    new Question
                    {
                        Text = "Q",
                        TimeLimitSeconds = 30,
                        Points = 100,
                        OrderIndex = 0,
                        AnswerOptions =
                        [
                            new AnswerOption { Text = "Right", IsCorrect = true },
                            new AnswerOption { Text = "Wrong", IsCorrect = false }
                        ]
                    }
                ]
            };

            db.Rooms.Add(new Room
            {
                Quiz = quiz,
                HostId = HostId,
                JoinCode = JoinCode,
                Status = RoomStatus.Waiting,
                Participants = [new Participant { DisplayName = "Anna", PlayerTokenHash = hash }]
            });
            await db.SaveChangesAsync();

            var correct = await db.AnswerOptions.SingleAsync(a => a.IsCorrect);
            return new Seed(token, correct.Id);
        }

        private static string HostJwt()
        {
            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey)),
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: JwtIssuer,
                audience: JwtIssuer,
                claims: [new Claim(JwtRegisteredClaimNames.Sub, HostId)],
                expires: DateTime.UtcNow.AddMinutes(5),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// /// <summary>
        /// WebSockets on purpose, and the token is appended to the socket URL as ?access_token= by hand,
        /// the way the browser client does it. This exercises the OnMessageReceived wiring in Program.cs.
        /// </summary>
        private static HubConnection Connect(QuattroLingoFactory factory, string? jwt = null)
        {
            var server = factory.Server;

            return new HubConnectionBuilder()
                .WithUrl(new Uri(server.BaseAddress, "/hubs/game"), options =>
                {
                    options.Transports = HttpTransportType.WebSockets;
                    options.HttpMessageHandlerFactory = _ => server.CreateHandler();
                    options.WebSocketFactory = async (context, ct) =>
                    {
                        var uri = jwt is null
                            ? context.Uri
                            : new Uri(context.Uri + (context.Uri.Query.Length > 0 ? "&" : "?")
                                      + "access_token=" + Uri.EscapeDataString(jwt));

                        return await server.CreateWebSocketClient().ConnectAsync(uri, ct);
                    };

                    if (jwt is not null)
                    {
                        options.AccessTokenProvider = () => Task.FromResult<string?>(jwt);
                    }
                })
                .Build();
        }

        /// <summary>Register BEFORE triggering the action that causes the event.</summary>
        private static Task<JsonElement> NextEvent(HubConnection connection, string method)
        {
            var tcs = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
            connection.On<JsonElement>(method, payload => tcs.TrySetResult(payload));
            return tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }

        [Fact]
        public async Task PlayerJoinsWithToken_AndHostJoinsWithJwtFromTheQueryString()
        {
            await using var factory = new FastGameFactory();
            var seed = await SeedAsync(factory.Services);
            await using var player = Connect(factory);
            await using var host = Connect(factory, HostJwt());
            await player.StartAsync();
            await host.StartAsync();

            var playerView = await player.InvokeAsync<JsonElement>("JoinRoom", JoinCode, seed.PlayerToken);
            var hostView = await host.InvokeAsync<JsonElement>("JoinRoom", JoinCode, (string?)null);

            Assert.False(playerView.GetProperty("isHost").GetBoolean());
            Assert.Equal("Anna", playerView.GetProperty("me").GetProperty("name").GetString());
            Assert.True(hostView.GetProperty("isHost").GetBoolean());
        }

        [Fact]
        public async Task JoinWithoutAValidToken_IsRejectedWithTheServerMessage()
        {
            await using var factory = new FastGameFactory();
            await SeedAsync(factory.Services);
            await using var stranger = Connect(factory);
            await stranger.StartAsync();

            var error = await Assert.ThrowsAsync<HubException>(
                () => stranger.InvokeAsync<JsonElement>("JoinRoom", JoinCode, "not-a-real-token"));

            // The filter turned our UnauthorizedException into a HubException with its message.
            Assert.Contains("Unknown player", error.Message);
        }

        [Fact]
        public async Task FullGame_OverTheRealHub_ScoresPersistsAndNeverLeaksTheAnswer()
        {
            await using var factory = new FastGameFactory();
            var seed = await SeedAsync(factory.Services);
            await using var host = Connect(factory, HostJwt());
            await using var player = Connect(factory);

            var questionStarted = NextEvent(player, "QuestionStarted");
            var questionEnded = NextEvent(player, "QuestionEnded");
            var gameFinished = NextEvent(player, "GameFinished");

            await host.StartAsync();
            await player.StartAsync();
            await player.InvokeAsync<JsonElement>("JoinRoom", JoinCode, seed.PlayerToken);
            await host.InvokeAsync<JsonElement>("JoinRoom", JoinCode, (string?)null);

            await host.InvokeAsync("StartGame");

            // The question as it travels over the wire never says which option is right.
            var question = await questionStarted;
            Assert.Equal(2, question.GetProperty("options").GetArrayLength());
            Assert.DoesNotContain("correct", question.GetRawText(), StringComparison.OrdinalIgnoreCase);

            // The invocation returns nothing: SubmitAnswer cannot reveal correctness.
            await player.InvokeAsync("SubmitAnswer", 0, seed.CorrectOptionId);

            var ended = await questionEnded;
            Assert.Equal(seed.CorrectOptionId, ended.GetProperty("correctOptionId").GetInt32());

            var finished = await gameFinished;
            var winner = finished.GetProperty("leaderboard")[0];
            Assert.Equal("Anna", winner.GetProperty("name").GetString());
            Assert.True(winner.GetProperty("score").GetInt32() > 0);

            // GameFinished is sent after the results are saved, so the database is already final.
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var room = await db.Rooms.Include(r => r.Participants).SingleAsync();
            Assert.Equal(RoomStatus.Finished, room.Status);
            Assert.True(room.Participants.Single().Score > 0);
        }
    }
}