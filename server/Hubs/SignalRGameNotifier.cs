using Microsoft.AspNetCore.SignalR;
using QuattroLingo.DTOs.Response.Game;
using QuattroLingo.Interfaces.Services;

namespace QuattroLingo.Hubs
{
    public class SignalRGameNotifier(IHubContext<GameHub> hub) : IGameNotifier
    {
        private static string Group(string joinCode) => $"room-{joinCode}";

        private Task Send(string joinCode, string method, object payload) =>
            hub.Clients.Group(Group(joinCode)).SendAsync(method, payload);

        public Task AddToRoomAsync(string connectionId, string joinCode) =>
            hub.Groups.AddToGroupAsync(connectionId, Group(joinCode));

        public Task RemoveFromRoomAsync(string connectionId, string joinCode) =>
            hub.Groups.RemoveFromGroupAsync(connectionId, Group(joinCode));

        public Task PlayerJoinedAsync(string joinCode, PlayerInfo player) =>
            Send(joinCode, "PlayerJoined", player);

        public Task PlayerLeftAsync(string joinCode, int participantId) =>
            Send(joinCode, "PlayerLeft", new { participantId });

        public Task GameStartedAsync(string joinCode, int totalQuestions, int startsInMs) =>
            Send(joinCode, "GameStarted", new { totalQuestions, startsInMs });

        public Task QuestionStartedAsync(string joinCode, QuestionInfo question) =>
            Send(joinCode, "QuestionStarted", question);

        public Task QuestionEndedAsync(string joinCode, QuestionResult result) =>
            Send(joinCode, "QuestionEnded", result);

        public Task ScoreUpdatedAsync(string joinCode, IReadOnlyList<LeaderboardEntry> leaderboard) =>
            Send(joinCode, "ScoreUpdated", new { leaderboard });

        public Task GameFinishedAsync(string joinCode, IReadOnlyList<LeaderboardEntry> leaderboard) =>
            Send(joinCode, "GameFinished", new { leaderboard });

        public Task GameCancelledAsync(string joinCode) =>
            hub.Clients.Group(Group(joinCode)).SendAsync("GameCancelled");
    }
}