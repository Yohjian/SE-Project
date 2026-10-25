using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using QuattroLingo.DTOs.Response.Game;
using QuattroLingo.Entities;
using QuattroLingo.Enums;
using QuattroLingo.Exceptions;
using QuattroLingo.Helpers;
using QuattroLingo.Interfaces.Repositories;
using QuattroLingo.Interfaces.Services;
using QuattroLingo.Models;
using QuattroLingo.Services;
using RoomEntity = QuattroLingo.Entities.Room;

namespace QuattroLingo.Tests.Services
{
    public class GameServiceTests
    {
        private sealed class RecordingNotifier : IGameNotifier
        {
            private readonly object _gate = new();
            private readonly List<string> _events = [];

            public QuestionInfo? LastQuestion { get; private set; }
            public QuestionResult? LastResult { get; private set; }
            public IReadOnlyList<LeaderboardEntry> LastScores { get; private set; } = [];
            public IReadOnlyList<LeaderboardEntry> FinalScores { get; private set; } = [];

            public List<string> Events
            {
                get { lock (_gate) { return [.. _events]; } }
            }

            private Task Record(string name)
            {
                lock (_gate) { _events.Add(name); }
                return Task.CompletedTask;
            }

            public async Task WaitForAsync(string name, int times = 1)
            {
                for (var i = 0; i < 500; i++)
                {
                    lock (_gate)
                    {
                        if (_events.Count(e => e == name) >= times) return;
                    }
                    await Task.Delay(10);
                }

                throw new TimeoutException($"Event '{name}' did not arrive. Seen: {string.Join(", ", Events)}");
            }

            public Task AddToRoomAsync(string connectionId, string joinCode) => Record($"AddToRoom:{connectionId}");
            public Task RemoveFromRoomAsync(string connectionId, string joinCode) => Record($"RemoveFromRoom:{connectionId}");
            public Task PlayerJoinedAsync(string joinCode, PlayerInfo player) => Record($"PlayerJoined:{player.Name}");
            public Task PlayerLeftAsync(string joinCode, int participantId) => Record($"PlayerLeft:{participantId}");
            public Task GameStartedAsync(string joinCode, int totalQuestions, int startsInMs) => Record("GameStarted");

            public Task QuestionStartedAsync(string joinCode, QuestionInfo question)
            {
                LastQuestion = question;
                return Record($"QuestionStarted:{question.Index}");
            }

            public Task QuestionEndedAsync(string joinCode, QuestionResult result)
            {
                LastResult = result;
                return Record($"QuestionEnded:{result.QuestionIndex}");
            }

            public Task ScoreUpdatedAsync(string joinCode, IReadOnlyList<LeaderboardEntry> leaderboard)
            {
                LastScores = leaderboard;
                return Record("ScoreUpdated");
            }

            public Task GameFinishedAsync(string joinCode, IReadOnlyList<LeaderboardEntry> leaderboard)
            {
                FinalScores = leaderboard;
                return Record("GameFinished");
            }

            public Task GameCancelledAsync(string joinCode) => Record("GameCancelled");
        }

        private sealed class Harness
        {
            public const string Code = "123456";

            public FakeTimeProvider Time { get; } = new();
            public RecordingNotifier Notifier { get; } = new();
            public IRoomRepository Rooms { get; } = Substitute.For<IRoomRepository>();
            public GameOptions Options { get; } = new();
            public GameService Service { get; }
            public string[] Players { get; }
            private Dictionary<string, string> Tokens { get; } = [];

            // Participant ids are 1, 2, ... in the order of `players`.
            public Harness(string[] players, int questionCount)
            {
                Players = players;

                var room = new RoomEntity
                {
                    Id = 1, QuizId = 1, HostId = "host-1", JoinCode = Code,
                    Status = RoomStatus.Waiting,
                    CreatedAt = Time.GetUtcNow().UtcDateTime
                };

                var id = 1;
                foreach (var name in players)
                {
                    var (token, hash) = PlayerTokens.Generate();
                    Tokens[name] = token;
                    room.Participants.Add(new Participant
                    {
                        Id = id++, RoomId = 1, DisplayName = name, PlayerTokenHash = hash
                    });
                }

                var quiz = new Quiz { Id = 1, OwnerId = "host-1", Title = "Test quiz" };
                for (var i = 0; i < questionCount; i++)
                {
                    var question = new Question
                    {
                        Id = i + 1, QuizId = 1, Text = $"Question {i}",
                        TimeLimitSeconds = 10, Points = 100, OrderIndex = i
                    };
                    for (var k = 1; k <= 3; k++)
                    {
                        question.AnswerOptions.Add(new AnswerOption
                        {
                            Id = (i + 1) * 10 + k, QuestionId = i + 1,
                            Text = $"Option {k}", IsCorrect = k == 1
                        });
                    }
                    quiz.Questions.Add(question);
                }

                Rooms.GetByJoinCodeAsync(Code).Returns(room);
                var quizzes = Substitute.For<IQuizRepository>();
                quizzes.GetByIdAsync(1).Returns(quiz);

                var services = new ServiceCollection();
                services.AddSingleton(Rooms);
                services.AddSingleton(quizzes);

                Service = new GameService(
                    services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
                    Notifier, Time, Options, NullLogger<GameService>.Instance);
            }

            public static int Correct(int question) => (question + 1) * 10 + 1;
            public static int Wrong(int question) => (question + 1) * 10 + 2;

            public Task<RoomSnapshot> JoinHost(string connectionId = "c-host") =>
                Service.JoinAsync(connectionId, Code, null, "host-1");

            public Task<RoomSnapshot> JoinPlayer(string name, string? connectionId = null) =>
                Service.JoinAsync(connectionId ?? $"c-{name}", Code, Tokens[name], null);

            public async Task StartAndOpenFirstQuestionAsync()
            {
                await JoinHost();
                foreach (var name in Players) await JoinPlayer(name);
                await Service.StartAsync("c-host");
                Time.Advance(Options.CountdownDelay);
                await Notifier.WaitForAsync("QuestionStarted:0");
            }
        }

        // ---------- Joining ----------

        [Fact]
        public async Task JoinPlayer_ReturnsSnapshotAndAnnouncesThem()
        {
            var h = new Harness(["Anna"], 1);

            var snapshot = await h.JoinPlayer("Anna");

            Assert.False(snapshot.IsHost);
            Assert.Equal("Anna", snapshot.Me!.Name);
            Assert.Equal(GamePhase.Waiting, snapshot.Phase);
            Assert.Contains("PlayerJoined:Anna", h.Notifier.Events);
            Assert.Contains("AddToRoom:c-Anna", h.Notifier.Events);
        }

        [Fact]
        public async Task JoinHost_ByHostUserId_ReturnsHostSnapshot()
        {
            var h = new Harness(["Anna"], 1);

            var snapshot = await h.JoinHost();

            Assert.True(snapshot.IsHost);
            Assert.Null(snapshot.Me);
            Assert.Single(snapshot.Players);
        }

        [Fact]
        public async Task Join_WrongToken_ThrowsUnauthorized()
        {
            var h = new Harness(["Anna"], 1);

            await Assert.ThrowsAsync<UnauthorizedException>(
                () => h.Service.JoinAsync("c-x", Harness.Code, "bogus-token", null));
        }

        [Fact]
        public async Task Join_NotTheHostAndNoToken_ThrowsUnauthorized()
        {
            var h = new Harness(["Anna"], 1);

            await Assert.ThrowsAsync<UnauthorizedException>(
                () => h.Service.JoinAsync("c-x", Harness.Code, null, "someone-else"));
        }

        [Fact]
        public async Task Join_UnknownRoom_ThrowsNotFound()
        {
            var h = new Harness(["Anna"], 1);

            await Assert.ThrowsAsync<NotFoundException>(
                () => h.Service.JoinAsync("c-x", "999999", null, "host-1"));
        }

        // ---------- Starting ----------

        [Fact]
        public async Task Start_ByPlayer_ThrowsForbidden()
        {
            var h = new Harness(["Anna"], 1);
            await h.JoinHost();
            await h.JoinPlayer("Anna");

            await Assert.ThrowsAsync<ForbiddenException>(() => h.Service.StartAsync("c-Anna"));
        }

        [Fact]
        public async Task Start_WithoutPlayers_ThrowsConflict()
        {
            var h = new Harness([], 1);
            await h.JoinHost();

            await Assert.ThrowsAsync<ConflictException>(() => h.Service.StartAsync("c-host"));
        }

        [Fact]
        public async Task Start_QuizWithoutQuestions_ThrowsValidation()
        {
            var h = new Harness(["Anna"], 0);
            await h.JoinHost();

            await Assert.ThrowsAsync<ValidationException>(() => h.Service.StartAsync("c-host"));
        }

        [Fact]
        public async Task Start_Twice_ThrowsConflict()
        {
            var h = new Harness(["Anna"], 1);
            await h.JoinHost();
            await h.JoinPlayer("Anna");
            await h.Service.StartAsync("c-host");

            await Assert.ThrowsAsync<ConflictException>(() => h.Service.StartAsync("c-host"));
        }

        // ---------- A whole game ----------

        [Fact]
        public async Task FullGame_ScoresRanksAndPersistsResults()
        {
            var h = new Harness(["Anna", "Bob"], 2);
            await h.StartAndOpenFirstQuestionAsync();

            // The payload never contains the answer.
            Assert.Equal(3, h.Notifier.LastQuestion!.Options.Count);

            // Question 0: Anna right, Bob wrong -> everyone answered, ends without waiting for the timer.
            await h.Service.SubmitAnswerAsync("c-Anna", 0, Harness.Correct(0));
            await h.Service.SubmitAnswerAsync("c-Bob", 0, Harness.Wrong(0));
            await h.Notifier.WaitForAsync("QuestionEnded:0");
            await h.Notifier.WaitForAsync("ScoreUpdated");

            Assert.Equal(Harness.Correct(0), h.Notifier.LastResult!.CorrectOptionId);
            Assert.Equal(1, h.Notifier.LastResult.OptionCounts[Harness.Correct(0)]);
            var anna = h.Notifier.LastScores.Single(e => e.Name == "Anna");
            var bob = h.Notifier.LastScores.Single(e => e.Name == "Bob");
            Assert.Equal(100, anna.Score);
            Assert.Equal(100, anna.PointsGained);
            Assert.Equal(1, anna.Rank);
            Assert.Equal(0, bob.Score);
            Assert.Equal(2, bob.Rank);

            // Question 1: both right.
            h.Time.Advance(h.Options.ReviewDelay);
            await h.Notifier.WaitForAsync("QuestionStarted:1");
            await h.Service.SubmitAnswerAsync("c-Bob", 1, Harness.Correct(1));
            await h.Service.SubmitAnswerAsync("c-Anna", 1, Harness.Correct(1));
            await h.Notifier.WaitForAsync("QuestionEnded:1");
            await h.Notifier.WaitForAsync("ScoreUpdated", 2);

            h.Time.Advance(h.Options.ReviewDelay);
            await h.Notifier.WaitForAsync("GameFinished");

            Assert.Equal(200, h.Notifier.FinalScores.Single(e => e.Name == "Anna").Score);
            Assert.Equal(100, h.Notifier.FinalScores.Single(e => e.Name == "Bob").Score);

            await h.Rooms.Received().SetStatusAsync(1, RoomStatus.InProgress, Arg.Any<DateTime?>(), Arg.Any<DateTime?>());
            await h.Rooms.Received().SaveScoresAsync(1,
                Arg.Is<IReadOnlyDictionary<int, int>>(d => d[1] == 200 && d[2] == 100));
            await h.Rooms.Received().SetStatusAsync(1, RoomStatus.Finished, Arg.Any<DateTime?>(), Arg.Any<DateTime?>());
        }

        [Fact]
        public async Task SlowerCorrectAnswer_ScoresFewerPoints()
        {
            var h = new Harness(["Anna"], 1);
            await h.StartAndOpenFirstQuestionAsync();

            h.Time.Advance(TimeSpan.FromSeconds(5));   // half of the 10 s limit
            await h.Service.SubmitAnswerAsync("c-Anna", 0, Harness.Correct(0));
            await h.Notifier.WaitForAsync("ScoreUpdated");

            var expected = Score.FromAnswerTime(TimeSpan.FromSeconds(5), 10, 100).Points;
            Assert.Equal(expected, h.Notifier.LastScores.Single().PointsGained);
            Assert.True(expected < 100);
        }

        [Fact]
        public async Task QuestionEndsWhenTimeRunsOut_AndLateAnswersAreRejected()
        {
            var h = new Harness(["Anna", "Bob"], 1);
            await h.StartAndOpenFirstQuestionAsync();

            h.Time.Advance(TimeSpan.FromSeconds(10));
            await h.Notifier.WaitForAsync("QuestionEnded:0");

            Assert.All(h.Notifier.LastResult!.OptionCounts.Values, count => Assert.Equal(0, count));
            await Assert.ThrowsAsync<ConflictException>(
                () => h.Service.SubmitAnswerAsync("c-Anna", 0, Harness.Correct(0)));
        }

        // ---------- Answer validation ----------

        [Fact]
        public async Task SubmitAnswer_Twice_ThrowsConflict()
        {
            var h = new Harness(["Anna", "Bob"], 1);
            await h.StartAndOpenFirstQuestionAsync();
            await h.Service.SubmitAnswerAsync("c-Anna", 0, Harness.Correct(0));

            await Assert.ThrowsAsync<ConflictException>(
                () => h.Service.SubmitAnswerAsync("c-Anna", 0, Harness.Wrong(0)));
        }

        [Fact]
        public async Task SubmitAnswer_InvalidOption_ThrowsValidation()
        {
            var h = new Harness(["Anna", "Bob"], 1);
            await h.StartAndOpenFirstQuestionAsync();

            await Assert.ThrowsAsync<ValidationException>(
                () => h.Service.SubmitAnswerAsync("c-Anna", 0, 9999));
        }

        [Fact]
        public async Task SubmitAnswer_WrongQuestionIndex_ThrowsConflict()
        {
            var h = new Harness(["Anna", "Bob"], 2);
            await h.StartAndOpenFirstQuestionAsync();

            await Assert.ThrowsAsync<ConflictException>(
                () => h.Service.SubmitAnswerAsync("c-Anna", 1, Harness.Correct(1)));
        }

        [Fact]
        public async Task SubmitAnswer_BeforeTheGameStarted_ThrowsConflict()
        {
            var h = new Harness(["Anna"], 1);
            await h.JoinPlayer("Anna");

            await Assert.ThrowsAsync<ConflictException>(
                () => h.Service.SubmitAnswerAsync("c-Anna", 0, Harness.Correct(0)));
        }

        [Fact]
        public async Task SubmitAnswer_ByHost_ThrowsForbidden()
        {
            var h = new Harness(["Anna", "Bob"], 1);
            await h.StartAndOpenFirstQuestionAsync();

            await Assert.ThrowsAsync<ForbiddenException>(
                () => h.Service.SubmitAnswerAsync("c-host", 0, Harness.Correct(0)));
        }

        [Fact]
        public void LivePayloads_NeverExposeIsCorrect()
        {
            Assert.DoesNotContain(typeof(OptionInfo).GetProperties(), p => p.Name == "IsCorrect");
            Assert.DoesNotContain(typeof(QuestionInfo).GetProperties(), p => p.Name == "IsCorrect");
        }

        // ---------- Reconnect / leaving ----------

        [Fact]
        public async Task Reconnect_MidQuestion_ReturnsSnapshotWithoutTheCorrectAnswer()
        {
            var h = new Harness(["Anna", "Bob"], 1);
            await h.StartAndOpenFirstQuestionAsync();
            await h.Service.SubmitAnswerAsync("c-Anna", 0, Harness.Correct(0));

            var snapshot = await h.JoinPlayer("Anna", "c-Anna-2");

            Assert.Equal(GamePhase.QuestionOpen, snapshot.Phase);
            Assert.Equal(0, snapshot.CurrentQuestion!.Index);
            Assert.Equal(Harness.Correct(0), snapshot.MyAnswerOptionId);
            Assert.Null(snapshot.CorrectOptionId);
            Assert.InRange(snapshot.RemainingMs!.Value, 1, 10_000);
        }

        [Fact]
        public async Task Disconnect_DuringGame_KeepsThePlayerAndAllowsReturning()
        {
            var h = new Harness(["Anna", "Bob"], 1);
            await h.StartAndOpenFirstQuestionAsync();

            await h.Service.DisconnectAsync("c-Anna");
            var snapshot = await h.JoinPlayer("Anna", "c-Anna-2");

            Assert.True(snapshot.Me!.Connected);
            await h.Rooms.DidNotReceive().RemoveParticipantAsync(Arg.Any<int>());
        }

        [Fact]
        public async Task Leave_WhileWaiting_RemovesTheParticipant()
        {
            var h = new Harness(["Anna"], 1);
            await h.JoinHost();
            await h.JoinPlayer("Anna");

            await h.Service.LeaveAsync("c-Anna");

            await h.Rooms.Received().RemoveParticipantAsync(1);
            Assert.Contains("PlayerLeft:1", h.Notifier.Events);
        }

        // ---------- Cleanup ----------

        [Fact]
        public async Task Cleanup_RemovesPlayersDisconnectedLongerThanTheGracePeriod()
        {
            var h = new Harness(["Anna"], 1);
            await h.JoinHost();
            await h.JoinPlayer("Anna");
            await h.Service.DisconnectAsync("c-Anna");

            h.Time.Advance(h.Options.DisconnectedPlayerGrace + TimeSpan.FromSeconds(1));
            await h.Service.CleanupAsync();

            await h.Rooms.Received().RemoveParticipantAsync(1);
            Assert.Contains("PlayerLeft:1", h.Notifier.Events);
        }

        [Fact]
        public async Task Cleanup_CancelsRoomEveryoneLeft()
        {
            var h = new Harness(["Anna"], 1);
            await h.JoinHost();
            await h.Service.DisconnectAsync("c-host");

            h.Time.Advance(h.Options.AbandonedGameTimeout + TimeSpan.FromSeconds(1));
            await h.Service.CleanupAsync();

            Assert.Contains("GameCancelled", h.Notifier.Events);
            await h.Rooms.Received().SetStatusAsync(1, RoomStatus.Cancelled, Arg.Any<DateTime?>(), Arg.Any<DateTime?>());
        }

        [Fact]
        public async Task Cleanup_CancelsWaitingRoomThatIsTooOld()
        {
            var h = new Harness(["Anna"], 1);
            await h.JoinHost();   // host stays connected, so only the age rule can fire

            h.Time.Advance(h.Options.WaitingRoomMaxAge + TimeSpan.FromSeconds(1));
            await h.Service.CleanupAsync();

            Assert.Contains("GameCancelled", h.Notifier.Events);
        }

        [Fact]
        public async Task Cleanup_LeavesActiveRoomsAlone()
        {
            var h = new Harness(["Anna"], 1);
            await h.JoinHost();
            await h.JoinPlayer("Anna");

            h.Time.Advance(TimeSpan.FromMinutes(1));
            await h.Service.CleanupAsync();

            Assert.DoesNotContain("GameCancelled", h.Notifier.Events);
            await h.Rooms.DidNotReceive().RemoveParticipantAsync(Arg.Any<int>());
        }
    }
}