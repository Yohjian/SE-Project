using Microsoft.AspNetCore.SignalR;
using NSubstitute;
using QuattroLingo.DTOs.Response.Game;
using QuattroLingo.Hubs;

namespace QuattroLingo.Tests.Hubs
{
    public class SignalRGameNotifierTests
    {
        private const string JoinCode = "ABC123";
        private const string RoomGroup = "room-ABC123";

        private readonly IHubContext<GameHub> _hub = Substitute.For<IHubContext<GameHub>>();
        private readonly IClientProxy _roomClients = Substitute.For<IClientProxy>();
        private readonly SignalRGameNotifier _notifier;

        private static readonly IReadOnlyList<LeaderboardEntry> Leaderboard =
        [
            new(1, "Ona", 900, 1, 500),
            new(2, "Jonas", 400, 2, 0)
        ];

        public SignalRGameNotifierTests()
        {
            _hub.Clients.Group(RoomGroup).Returns(_roomClients);
            _notifier = new SignalRGameNotifier(_hub);
        }

        /// <summary>SendAsync is an extension method; what actually reaches the proxy is SendCoreAsync.</summary>
        private object? SentPayload(string method)
        {
            var call = _roomClients.ReceivedCalls()
                .Single(c => c.GetMethodInfo().Name == nameof(IClientProxy.SendCoreAsync)
                             && (string)c.GetArguments()[0]! == method);

            var args = (object?[])call.GetArguments()[1]!;
            return Assert.Single(args);
        }

        private static object? Property(object? payload, string name) =>
            payload!.GetType().GetProperty(name)?.GetValue(payload);

        // ---- AddToRoomAsync / RemoveFromRoomAsync ----

        [Fact]
        public async Task AddToRoomAsync_ConnectionAndJoinCode_AddsConnectionToRoomGroup()
        {
            await _notifier.AddToRoomAsync("conn-1", JoinCode);

            await _hub.Groups.Received(1).AddToGroupAsync("conn-1", RoomGroup, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task RemoveFromRoomAsync_ConnectionAndJoinCode_RemovesConnectionFromRoomGroup()
        {
            await _notifier.RemoveFromRoomAsync("conn-1", JoinCode);

            await _hub.Groups.Received(1).RemoveFromGroupAsync("conn-1", RoomGroup, Arg.Any<CancellationToken>());
        }

        // ---- Broadcasts ----

        [Fact]
        public async Task PlayerJoinedAsync_NewPlayer_SendsPlayerJoinedWithPlayerInfoToRoomOnly()
        {
            var player = new PlayerInfo(5, "Ona", 0, true);

            await _notifier.PlayerJoinedAsync(JoinCode, player);

            Assert.Same(player, SentPayload("PlayerJoined"));
            _hub.Clients.DidNotReceive().Group(Arg.Is<string>(g => g != RoomGroup));
        }

        [Fact]
        public async Task PlayerLeftAsync_ParticipantId_SendsPlayerLeftWithParticipantId()
        {
            await _notifier.PlayerLeftAsync(JoinCode, 5);

            Assert.Equal(5, Property(SentPayload("PlayerLeft"), "participantId"));
        }

        [Fact]
        public async Task GameStartedAsync_TotalAndDelay_SendsGameStartedWithTotalQuestionsAndStartsInMs()
        {
            await _notifier.GameStartedAsync(JoinCode, totalQuestions: 10, startsInMs: 3000);

            var payload = SentPayload("GameStarted");
            Assert.Equal(10, Property(payload, "totalQuestions"));
            Assert.Equal(3000, Property(payload, "startsInMs"));
        }

        [Fact]
        public async Task QuestionStartedAsync_Question_SendsQuestionStartedWithQuestionInfo()
        {
            var question = new QuestionInfo(0, 10, "Labas means?", [new(1, "Hello"), new(2, "Bye")], 20000);

            await _notifier.QuestionStartedAsync(JoinCode, question);

            Assert.Same(question, SentPayload("QuestionStarted"));
        }

        [Fact]
        public async Task QuestionEndedAsync_Result_SendsQuestionEndedWithResult()
        {
            var result = new QuestionResult(0, 1, new Dictionary<int, int> { [1] = 3, [2] = 1 });

            await _notifier.QuestionEndedAsync(JoinCode, result);

            Assert.Same(result, SentPayload("QuestionEnded"));
        }

        [Fact]
        public async Task ScoreUpdatedAsync_Leaderboard_SendsScoreUpdatedWithLeaderboard()
        {
            await _notifier.ScoreUpdatedAsync(JoinCode, Leaderboard);

            Assert.Same(Leaderboard, Property(SentPayload("ScoreUpdated"), "leaderboard"));
        }

        [Fact]
        public async Task GameFinishedAsync_Leaderboard_SendsGameFinishedWithLeaderboard()
        {
            await _notifier.GameFinishedAsync(JoinCode, Leaderboard);

            Assert.Same(Leaderboard, Property(SentPayload("GameFinished"), "leaderboard"));
        }

        [Fact]
        public async Task GameCancelledAsync_JoinCode_SendsGameCancelledWithoutPayload()
        {
            await _notifier.GameCancelledAsync(JoinCode);

            await _roomClients.Received(1).SendCoreAsync(
                "GameCancelled",
                Arg.Is<object?[]>(args => args.Length == 0),
                Arg.Any<CancellationToken>());
        }
    }
}
