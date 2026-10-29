using System.Collections.Concurrent;
using QuattroLingo.DTOs.Response.Game;
using QuattroLingo.Enums;
using QuattroLingo.Exceptions;
using QuattroLingo.Helpers;
using QuattroLingo.Interfaces.Repositories;
using QuattroLingo.Interfaces.Services;
using QuattroLingo.Models;

namespace QuattroLingo.Services
{
    public class GameService : IGameService
    {
        private readonly ConcurrentDictionary<string, LiveGame> _games = new();
        private readonly ConcurrentDictionary<string, ConnectionInfo> _connections = new();
        private readonly SemaphoreSlim _loadLock = new(1, 1);

        private readonly IServiceScopeFactory _scopes;
        private readonly IGameNotifier _notifier;
        private readonly TimeProvider _time;
        private readonly GameOptions _options;
        private readonly ILogger<GameService> _logger;

        public GameService(
            IServiceScopeFactory scopes,
            IGameNotifier notifier,
            TimeProvider time,
            GameOptions options,
            ILogger<GameService> logger)
        {
            _scopes = scopes;
            _notifier = notifier;
            _time = time;
            _options = options;
            _logger = logger;
        }

        // ---------- Joining / leaving ----------

        public async Task<RoomSnapshot> JoinAsync(
            string connectionId, string joinCode, string? playerToken, string? hostUserId)
        {
            if (_connections.ContainsKey(connectionId))
            {
                throw new ConflictException("This connection is already in a room.");
            }

            var game = await GetOrLoadAsync(joinCode);
            PlayerInfo? announce = null;
            RoomSnapshot snapshot;

            using (await game.LockAsync())
            {
                if (game.Phase == GamePhase.Cancelled)
                {
                    throw new ConflictException("This game was cancelled.");
                }

                var isHost = hostUserId is not null && hostUserId == game.HostId;
                LivePlayer? player = null;

                if (!isHost)
                {
                    player = FindPlayer(game, playerToken);

                    // A player may have joined over REST after this game was loaded into memory.
                    if (player is null && game.Phase == GamePhase.Waiting && !string.IsNullOrEmpty(playerToken))
                    {
                        await SyncParticipantsAsync(game);
                        player = FindPlayer(game, playerToken);
                    }

                    if (player is null)
                    {
                        throw new UnauthorizedException("Unknown player. Join the room first.");
                    }
                }

                if (player is null)
                {
                    game.HostConnections++;
                }
                else
                {
                    player.Connected = true;
                    player.ConnectionId = connectionId;
                    player.DisconnectedAt = null;

                    if (!player.Announced)
                    {
                        player.Announced = true;
                        announce = ToPlayerInfo(player);
                    }
                }

                UpdateEmptiness(game, _time.GetUtcNow());
                _connections[connectionId] = new ConnectionInfo(game.JoinCode, player?.Id);

                // Join the SignalR group before building the snapshot.
                await _notifier.AddToRoomAsync(connectionId, game.JoinCode);

                snapshot = BuildSnapshot(game, player, isHost);
            }

            if (announce is not null)
            {
                await _notifier.PlayerJoinedAsync(game.JoinCode, announce);
            }

            return snapshot;
        }

        public async Task LeaveAsync(string connectionId)
        {
            if (!_connections.TryRemove(connectionId, out var info)) return;
            if (!_games.TryGetValue(info.JoinCode, out var game)) return;

            int? removedPlayerId = null;

            using (await game.LockAsync())
            {
                // While the lobby is open a leaving player is removed; later they only disconnect.
                if (info.ParticipantId is { } id
                    && game.Phase == GamePhase.Waiting
                    && game.Players.TryGetValue(id, out var player)
                    && player.ConnectionId == connectionId)
                {
                    game.Players.Remove(id);
                    removedPlayerId = id;
                    UpdateEmptiness(game, _time.GetUtcNow());
                }
                else
                {
                    MarkDisconnected(game, info, connectionId);
                }
            }

            await _notifier.RemoveFromRoomAsync(connectionId, info.JoinCode);

            if (removedPlayerId is { } removedId)
            {
                await DeleteParticipantAsync(removedId);
                await _notifier.PlayerLeftAsync(info.JoinCode, removedId);
            }
        }

        public async Task DisconnectAsync(string connectionId)
        {
            if (!_connections.TryRemove(connectionId, out var info)) return;
            if (!_games.TryGetValue(info.JoinCode, out var game)) return;

            using (await game.LockAsync())
            {
                MarkDisconnected(game, info, connectionId);
            }
        }

        // ---------- Playing ----------

        public async Task StartAsync(string connectionId)
        {
            var (game, info) = RequireConnection(connectionId);

            if (info.ParticipantId is not null)
            {
                throw new ForbiddenException("Only the host can start the game.");
            }

            using (await game.LockAsync())
            {
                if (game.Phase != GamePhase.Waiting)
                {
                    throw new ConflictException("The game has already started.");
                }

                if (game.Questions.Count == 0)
                {
                    throw new ValidationException("This quiz has no questions.");
                }

                if (game.Players.Count == 0)
                {
                    throw new ConflictException("At least one player is needed to start the game.");
                }

                game.Phase = GamePhase.Countdown;
            }

            try
            {
                await WithRoomsAsync(r => r.SetStatusAsync(
                    game.RoomId, RoomStatus.InProgress, startedAt: _time.GetUtcNow().UtcDateTime));
            }
            catch
            {
                using (await game.LockAsync())
                {
                    game.Phase = GamePhase.Waiting;
                }
                throw;
            }

            await _notifier.GameStartedAsync(
                game.JoinCode, game.Questions.Count, (int)_options.CountdownDelay.TotalMilliseconds);

            // Runs synchronously up to its first await, so the countdown timer already
            // exists when StartAsync returns. Everything else continues in the background.
            _ = RunGameAsync(game);
        }

        public async Task SubmitAnswerAsync(string connectionId, int questionIndex, int optionId)
        {
            var (game, info) = RequireConnection(connectionId);

            if (info.ParticipantId is not { } playerId)
            {
                throw new ForbiddenException("The host does not play.");
            }

            using (await game.LockAsync())
            {
                if (!game.Players.ContainsKey(playerId))
                {
                    throw new ForbiddenException("You are not part of this game.");
                }

                if (game.Phase != GamePhase.QuestionOpen || game.QuestionIndex != questionIndex)
                {
                    throw new ConflictException("This question is not open for answers.");
                }

                var now = _time.GetUtcNow();

                if (now > game.QuestionEndsAt)
                {
                    throw new ConflictException("Time is up.");
                }

                if (game.Answers.ContainsKey(playerId))
                {
                    throw new ConflictException("You already answered this question.");
                }

                var question = game.Questions[questionIndex];
                var option = question.Options.FirstOrDefault(o => o.Id == optionId)
                    ?? throw new ValidationException("Invalid answer option.");

                // Elapsed time is measured on the server clock, never reported by the client.
                var points = option.Id == question.CorrectOptionId
                    ? Score.FromAnswerTime(now - game.QuestionStartedAt, question.TimeLimitSeconds, question.Points).Points
                    : 0;

                game.Answers[playerId] = new LiveAnswer(option.Id, now, points);
                TrySignalAllAnswered(game);
            }
        }

        // ---------- The server-driven loop ----------

        private async Task RunGameAsync(LiveGame game)
        {
            var ct = game.Cts.Token;

            try
            {
                var countdown = Task.Delay(_options.CountdownDelay, _time, ct);
                await countdown;

                for (var index = 0; index < game.Questions.Count; index++)
                {
                    await RunQuestionAsync(game, index, ct);
                }

                await FinishAsync(game);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Cancelled by CancelGameAsync; it already did the bookkeeping.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Game {JoinCode} crashed and will be cancelled.", game.JoinCode);
                await CancelGameAsync(game);
            }
        }

        private async Task RunQuestionAsync(LiveGame game, int index, CancellationToken ct)
        {
            var question = game.Questions[index];
            QuestionInfo info;
            TaskCompletionSource allAnswered;

            using (await game.LockAsync(ct))
            {
                game.QuestionIndex = index;
                game.Answers.Clear();
                game.QuestionStartedAt = _time.GetUtcNow();
                game.QuestionEndsAt = game.QuestionStartedAt.AddSeconds(question.TimeLimitSeconds);
                allAnswered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                game.AllAnswered = allAnswered;
                game.Phase = GamePhase.QuestionOpen;
                info = ToQuestionInfo(game, index);
            }

            using var questionCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            // Timer first, announcement second: when clients (or tests) see the event,
            // the clock is already running.
            var timeout = Task.Delay(TimeSpan.FromSeconds(question.TimeLimitSeconds), _time, questionCts.Token);
            await _notifier.QuestionStartedAsync(game.JoinCode, info);

            await Task.WhenAny(timeout, allAnswered.Task);
            await questionCts.CancelAsync();
            ct.ThrowIfCancellationRequested();

            QuestionResult result;
            List<LeaderboardEntry> board;

            using (await game.LockAsync(ct))
            {
                game.Phase = GamePhase.QuestionReview;

                foreach (var (playerId, answer) in game.Answers)
                {
                    if (game.Players.TryGetValue(playerId, out var player))
                    {
                        player.Score += answer.Points;
                    }
                }

                var counts = question.Options.ToDictionary(
                    o => o.Id,
                    o => game.Answers.Values.Count(a => a.OptionId == o.Id));

                result = new QuestionResult(index, question.CorrectOptionId, counts);
                board = BuildLeaderboard(game);
            }

            var review = Task.Delay(_options.ReviewDelay, _time, ct);
            await _notifier.QuestionEndedAsync(game.JoinCode, result);
            await _notifier.ScoreUpdatedAsync(game.JoinCode, board);
            await review;
        }

        private async Task FinishAsync(LiveGame game)
        {
            var now = _time.GetUtcNow();
            Dictionary<int, int> scores;
            List<LeaderboardEntry> board;

            using (await game.LockAsync())
            {
                game.Phase = GamePhase.Finished;
                game.FinishedAt = now;
                scores = game.Players.Values.ToDictionary(p => p.Id, p => p.Score);
                board = BuildLeaderboard(game);
            }

            try
            {
                await WithRoomsAsync(async rooms =>
                {
                    await rooms.SaveScoresAsync(game.RoomId, scores);
                    await rooms.SetStatusAsync(game.RoomId, RoomStatus.Finished, finishedAt: now.UtcDateTime);
                });
            }
            catch (Exception ex)
            {
                // Players still get their results; only the persisted copy is lost.
                _logger.LogError(ex, "Could not save the results of game {JoinCode}.", game.JoinCode);
            }

            await _notifier.GameFinishedAsync(game.JoinCode, board);
        }

        private async Task CancelGameAsync(LiveGame game)
        {
            using (await game.LockAsync())
            {
                if (game.Phase is GamePhase.Cancelled or GamePhase.Finished) return;
                game.Phase = GamePhase.Cancelled;
            }

            game.Cts.Cancel();

            try
            {
                await WithRoomsAsync(r => r.SetStatusAsync(
                    game.RoomId, RoomStatus.Cancelled, finishedAt: _time.GetUtcNow().UtcDateTime));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not mark game {JoinCode} as cancelled.", game.JoinCode);
            }

            await _notifier.GameCancelledAsync(game.JoinCode);
            RemoveGame(game);
        }

        // ---------- Housekeeping ----------

        public async Task CleanupAsync(CancellationToken ct = default)
        {
            var now = _time.GetUtcNow();

            foreach (var game in _games.Values.ToList())
            {
                var cancel = false;
                var drop = false;
                List<int> removedPlayers = [];

                using (await game.LockAsync(ct))
                {
                    switch (game.Phase)
                    {
                        case GamePhase.Waiting:
                            var stale = game.Players.Values
                                .Where(p => !p.Connected
                                    && p.DisconnectedAt is { } at
                                    && now - at > _options.DisconnectedPlayerGrace)
                                .ToList();

                            foreach (var player in stale)
                            {
                                game.Players.Remove(player.Id);
                                removedPlayers.Add(player.Id);
                            }

                            cancel = now - game.CreatedAt > _options.WaitingRoomMaxAge || IsAbandoned(game, now);
                            break;

                        case GamePhase.Countdown:
                        case GamePhase.QuestionOpen:
                        case GamePhase.QuestionReview:
                            cancel = IsAbandoned(game, now);
                            break;

                        case GamePhase.Finished:
                            drop = game.FinishedAt is { } finished && now - finished > _options.FinishedRetention;
                            break;

                        case GamePhase.Cancelled:
                            drop = true;
                            break;
                    }
                }

                foreach (var id in removedPlayers)
                {
                    await DeleteParticipantAsync(id);
                    await _notifier.PlayerLeftAsync(game.JoinCode, id);
                }

                if (cancel) await CancelGameAsync(game);
                else if (drop) RemoveGame(game);
            }

            // Waiting rooms nobody ever opened live only in the database.
            await WithRoomsAsync(r => r.CancelWaitingRoomsOlderThanAsync(
                now.UtcDateTime - _options.WaitingRoomMaxAge));
        }

        public Task ReconcileOnStartupAsync() =>
            WithRoomsAsync(r => r.CancelInProgressRoomsAsync());

        // ---------- Helpers ----------

        private async Task<LiveGame> GetOrLoadAsync(string joinCode)
        {
            var code = joinCode?.Trim() ?? string.Empty;

            if (_games.TryGetValue(code, out var existing)) return existing;

            await _loadLock.WaitAsync();
            try
            {
                if (_games.TryGetValue(code, out existing)) return existing;

                using var scope = _scopes.CreateScope();
                var rooms = scope.ServiceProvider.GetRequiredService<IRoomRepository>();
                var quizzes = scope.ServiceProvider.GetRequiredService<IQuizRepository>();

                var room = await rooms.GetByJoinCodeAsync(code)
                    ?? throw new NotFoundException("Room not found.");

                if (room.Status != RoomStatus.Waiting)
                {
                    throw new ConflictException("This game is no longer available.");
                }

                var quiz = await quizzes.GetByIdAsync(room.QuizId)
                    ?? throw new NotFoundException("Quiz not found.");

                var now = _time.GetUtcNow();

                // Snapshot of the quiz: later edits cannot affect a running game.
                var questions = quiz.Questions
                    .OrderBy(q => q.OrderIndex).ThenBy(q => q.Id)
                    .Select(q => new LiveQuestion(
                        q.Id,
                        q.Text,
                        Math.Max(1, q.TimeLimitSeconds),
                        q.Points,
                        q.AnswerOptions.OrderBy(a => a.Id).Select(a => new LiveOption(a.Id, a.Text)).ToList(),
                        q.AnswerOptions.First(a => a.IsCorrect).Id))
                    .ToList();

                var game = new LiveGame
                {
                    RoomId = room.Id,
                    JoinCode = room.JoinCode,
                    HostId = room.HostId,
                    QuizTitle = quiz.Title,
                    Questions = questions,
                    CreatedAt = new DateTimeOffset(DateTime.SpecifyKind(room.CreatedAt, DateTimeKind.Utc)),
                    EmptySince = now
                };

                foreach (var participant in room.Participants)
                {
                    game.Players[participant.Id] = new LivePlayer(
                        participant.Id, participant.DisplayName, participant.PlayerTokenHash)
                    { DisconnectedAt = now };
                }

                _games[code] = game;
                return game;
            }
            finally
            {
                _loadLock.Release();
            }
        }

        private async Task SyncParticipantsAsync(LiveGame game)
        {
            using var scope = _scopes.CreateScope();
            var rooms = scope.ServiceProvider.GetRequiredService<IRoomRepository>();
            var room = await rooms.GetByJoinCodeAsync(game.JoinCode);
            if (room is null) return;

            var now = _time.GetUtcNow();

            foreach (var participant in room.Participants.Where(p => !game.Players.ContainsKey(p.Id)))
            {
                game.Players[participant.Id] = new LivePlayer(
                    participant.Id, participant.DisplayName, participant.PlayerTokenHash)
                { DisconnectedAt = now };
            }
        }

        private (LiveGame Game, ConnectionInfo Info) RequireConnection(string connectionId)
        {
            if (!_connections.TryGetValue(connectionId, out var info)
                || !_games.TryGetValue(info.JoinCode, out var game))
            {
                throw new ConflictException("You are not in a room. Join a room first.");
            }

            return (game, info);
        }

        private static LivePlayer? FindPlayer(LiveGame game, string? token) =>
            game.Players.Values.FirstOrDefault(p => PlayerTokens.Matches(token, p.TokenHash));

        private void MarkDisconnected(LiveGame game, ConnectionInfo info, string connectionId)
        {
            var now = _time.GetUtcNow();

            if (info.ParticipantId is { } id)
            {
                // Ignore stale connections: the player may already be on a newer one.
                if (game.Players.TryGetValue(id, out var player) && player.ConnectionId == connectionId)
                {
                    player.Connected = false;
                    player.ConnectionId = null;
                    player.DisconnectedAt = now;
                }
            }
            else
            {
                game.HostConnections = Math.Max(0, game.HostConnections - 1);
            }

            UpdateEmptiness(game, now);
            TrySignalAllAnswered(game);
        }

        private static void UpdateEmptiness(LiveGame game, DateTimeOffset now)
        {
            var empty = game.HostConnections == 0 && game.Players.Values.All(p => !p.Connected);

            if (empty) game.EmptySince ??= now;
            else game.EmptySince = null;
        }

        private bool IsAbandoned(LiveGame game, DateTimeOffset now) =>
            game.EmptySince is { } since && now - since > _options.AbandonedGameTimeout;

        private static void TrySignalAllAnswered(LiveGame game)
        {
            if (game.Phase != GamePhase.QuestionOpen || game.AllAnswered is null) return;

            var connected = game.Players.Values.Where(p => p.Connected).ToList();

            if (connected.Count > 0 && connected.All(p => game.Answers.ContainsKey(p.Id)))
            {
                game.AllAnswered.TrySetResult();
            }
        }

        private void RemoveGame(LiveGame game)
        {
            _games.TryRemove(game.JoinCode, out _);

            foreach (var connection in _connections.Where(c => c.Value.JoinCode == game.JoinCode).ToList())
            {
                _connections.TryRemove(connection.Key, out _);
            }
        }

        private async Task WithRoomsAsync(Func<IRoomRepository, Task> action)
        {
            using var scope = _scopes.CreateScope();
            await action(scope.ServiceProvider.GetRequiredService<IRoomRepository>());
        }

        private async Task DeleteParticipantAsync(int participantId)
        {
            try
            {
                await WithRoomsAsync(r => r.RemoveParticipantAsync(participantId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not remove participant {ParticipantId}.", participantId);
            }
        }

        private static PlayerInfo ToPlayerInfo(LivePlayer p) => new(p.Id, p.Name, p.Score, p.Connected);

        private static QuestionInfo ToQuestionInfo(LiveGame game, int index)
        {
            var q = game.Questions[index];
            return new QuestionInfo(
                index,
                game.Questions.Count,
                q.Text,
                q.Options.Select(o => new OptionInfo(o.Id, o.Text)).ToList(),
                q.TimeLimitSeconds * 1000);
        }

        private static List<LeaderboardEntry> BuildLeaderboard(LiveGame game)
        {
            var ordered = game.Players.Values
                .OrderByDescending(p => p.Score)
                .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return ordered.Select(p => new LeaderboardEntry(
                p.Id,
                p.Name,
                p.Score,
                1 + ordered.Count(o => o.Score > p.Score),
                game.Answers.TryGetValue(p.Id, out var answer) ? answer.Points : 0)).ToList();
        }

        private RoomSnapshot BuildSnapshot(LiveGame game, LivePlayer? me, bool isHost)
        {
            var hasQuestion = game.Phase is GamePhase.QuestionOpen or GamePhase.QuestionReview
                && game.QuestionIndex >= 0;

            int? remainingMs = game.Phase == GamePhase.QuestionOpen
                ? (int)Math.Max(0, (game.QuestionEndsAt - _time.GetUtcNow()).TotalMilliseconds)
                : null;

            int? myAnswer = me is not null && game.Answers.TryGetValue(me.Id, out var answer)
                ? answer.OptionId
                : null;

            // The correct option is only revealed once the question is over.
            int? correct = game.Phase == GamePhase.QuestionReview
                ? game.Questions[game.QuestionIndex].CorrectOptionId
                : null;

            return new RoomSnapshot(
                game.JoinCode,
                game.QuizTitle,
                game.Phase,
                isHost,
                me is null ? null : ToPlayerInfo(me),
                game.Players.Values.Select(ToPlayerInfo).ToList(),
                game.Questions.Count,
                hasQuestion ? ToQuestionInfo(game, game.QuestionIndex) : null,
                remainingMs,
                myAnswer,
                correct,
                BuildLeaderboard(game));
        }
    }
}