namespace QuattroLingo.Services
{
    public class GameOptions
    {
        public TimeSpan CountdownDelay { get; init; } = TimeSpan.FromSeconds(3);
        public TimeSpan ReviewDelay { get; init; } = TimeSpan.FromSeconds(5);
        public TimeSpan WaitingRoomMaxAge { get; init; } = TimeSpan.FromMinutes(60);
        public TimeSpan DisconnectedPlayerGrace { get; init; } = TimeSpan.FromSeconds(60);
        public TimeSpan AbandonedGameTimeout { get; init; } = TimeSpan.FromMinutes(5);
        public TimeSpan FinishedRetention { get; init; } = TimeSpan.FromMinutes(10);
        public TimeSpan CleanupInterval { get; init; } = TimeSpan.FromSeconds(30);
    }
}