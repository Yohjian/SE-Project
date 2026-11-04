using QuattroLingo.Enums;

namespace QuattroLingo.Services
{
    internal sealed class LivePlayer(int id, string name, string tokenHash)
    {
        public int Id { get; } = id;
        public string Name { get; } = name;
        public string TokenHash { get; } = tokenHash;
        public int Score { get; set; }
        public bool Connected { get; set; }
        public bool Announced { get; set; }          // PlayerJoined already broadcast
        public string? ConnectionId { get; set; }    // the CURRENT connection (changes on reconnect)
        public DateTimeOffset? DisconnectedAt { get; set; }
    }

    internal sealed record LiveOption(int Id, string Text);

    internal sealed record LiveQuestion(
        int Id, string Text, int TimeLimitSeconds, int Points,
        IReadOnlyList<LiveOption> Options, int CorrectOptionId);

    internal sealed record LiveAnswer(int OptionId, DateTimeOffset At, int Points);

    /// <summary>JoinCode of the game a connection is in; ParticipantId is null for the host.</summary>
    internal sealed record ConnectionInfo(string JoinCode, int? ParticipantId);

    internal sealed class LiveGame
    {
        public required int RoomId { get; init; }
        public required string JoinCode { get; init; }
        public required string HostId { get; init; }
        public required string QuizTitle { get; init; }
        public required IReadOnlyList<LiveQuestion> Questions { get; init; }
        public required DateTimeOffset CreatedAt { get; init; }

        public Dictionary<int, LivePlayer> Players { get; } = [];
        public GamePhase Phase { get; set; } = GamePhase.Waiting;

        public int QuestionIndex { get; set; } = -1;
        public DateTimeOffset QuestionStartedAt { get; set; }
        public DateTimeOffset QuestionEndsAt { get; set; }
        public Dictionary<int, LiveAnswer> Answers { get; } = [];   // current question, by participant id
        public TaskCompletionSource? AllAnswered { get; set; }

        public int HostConnections { get; set; }
        public DateTimeOffset? EmptySince { get; set; }
        public DateTimeOffset? FinishedAt { get; set; }

        public CancellationTokenSource Cts { get; } = new();
        private SemaphoreSlim Gate { get; } = new(1, 1);

        public async Task<IDisposable> LockAsync(CancellationToken ct = default)
        {
            await Gate.WaitAsync(ct);
            return new Releaser(Gate);
        }

        private sealed class Releaser(SemaphoreSlim gate) : IDisposable
        {
            public void Dispose() => gate.Release();
        }
    }
}