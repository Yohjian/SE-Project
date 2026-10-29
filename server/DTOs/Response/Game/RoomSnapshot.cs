using QuattroLingo.Enums;

namespace QuattroLingo.DTOs.Response.Game
{
    public record RoomSnapshot(
        string JoinCode,
        string QuizTitle,
        GamePhase Phase,
        bool IsHost,
        PlayerInfo? Me,
        IReadOnlyList<PlayerInfo> Players,
        int TotalQuestions,
        QuestionInfo? CurrentQuestion,
        int? RemainingMs,
        int? MyAnswerOptionId,
        int? CorrectOptionId,      // only filled during QuestionReview
        IReadOnlyList<LeaderboardEntry> Leaderboard);
}
