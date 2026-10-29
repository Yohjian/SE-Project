using QuattroLingo.DTOs.Response.Game;

namespace QuattroLingo.Interfaces.Services
{
    public interface IGameNotifier
    {
        Task AddToRoomAsync(string connectionId, string joinCode);
        Task RemoveFromRoomAsync(string connectionId, string joinCode);

        Task PlayerJoinedAsync(string joinCode, PlayerInfo player);
        Task PlayerLeftAsync(string joinCode, int participantId);
        Task GameStartedAsync(string joinCode, int totalQuestions, int startsInMs);
        Task QuestionStartedAsync(string joinCode, QuestionInfo question);
        Task QuestionEndedAsync(string joinCode, QuestionResult result);
        Task ScoreUpdatedAsync(string joinCode, IReadOnlyList<LeaderboardEntry> leaderboard);
        Task GameFinishedAsync(string joinCode, IReadOnlyList<LeaderboardEntry> leaderboard);
        Task GameCancelledAsync(string joinCode);
    }
}