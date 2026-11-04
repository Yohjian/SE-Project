namespace QuattroLingo.DTOs.Response.Game
{
    public record LeaderboardEntry(int ParticipantId, string Name, int Score, int Rank, int PointsGained);
}
