using Microsoft.EntityFrameworkCore;
using QuattroLingo.Data;
using QuattroLingo.Entities;
using QuattroLingo.Enums;
using QuattroLingo.Repositories;

namespace QuattroLingo.Tests.Repositories
{
    public class RoomRepositoryTests : IAsyncLifetime
    {
        private readonly AppDbContext _db = TestDb.Create();
        private readonly RoomRepository _repository;

        public RoomRepositoryTests()
        {
            _repository = new RoomRepository(_db);
        }

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync() => await _db.DisposeAsync();

        private static Room MakeRoom(int id, string code, RoomStatus status = RoomStatus.Waiting,
            Visibility visibility = Visibility.Public, string hostId = "host-1", DateTime? createdAt = null) => new()
        {
            Id = id,
            QuizId = 1,
            HostId = hostId,
            JoinCode = code,
            Status = status,
            RoomVisibility = visibility,
            CreatedAt = createdAt ?? DateTime.UtcNow
        };

        private async Task SeedAsync(params Room[] rooms)
        {
            _db.Quizzes.Add(new Quiz { Id = 1, OwnerId = "host-1", Title = "English Quiz" });
            _db.Rooms.AddRange(rooms);
            await _db.SaveChangesAsync();
            _db.ChangeTracker.Clear();   // make the repository load from the store, not the tracker
        }

        // ---- GetByJoinCodeAsync ----

        [Fact]
        public async Task GetByJoinCodeAsync_ExistingCode_IncludesQuizAndParticipants()
        {
            var room = MakeRoom(1, "123456");
            room.Participants = [new Participant { DisplayName = "Anna" }, new Participant { DisplayName = "Ben" }];
            await SeedAsync(room);

            var loaded = await _repository.GetByJoinCodeAsync("123456");

            Assert.NotNull(loaded);
            Assert.Equal("English Quiz", loaded.Quiz!.Title);
            Assert.Equal(2, loaded.Participants.Count);
        }

        [Fact]
        public async Task GetByJoinCodeAsync_UnknownCode_ReturnsNull()
        {
            await SeedAsync(MakeRoom(1, "123456"));

            Assert.Null(await _repository.GetByJoinCodeAsync("654321"));
        }

        // ---- GetPublicWaitingRoomsAsync ----

        [Fact]
        public async Task GetPublicWaitingRoomsAsync_MixedRooms_ReturnsOnlyPublicWaitingRooms()
        {
            await SeedAsync(
                MakeRoom(1, "111111"),
                MakeRoom(2, "222222", visibility: Visibility.Private),
                MakeRoom(3, "333333", status: RoomStatus.InProgress),
                MakeRoom(4, "444444", status: RoomStatus.Finished));

            var rooms = await _repository.GetPublicWaitingRoomsAsync();

            Assert.Equal("111111", Assert.Single(rooms).JoinCode);
        }

        [Fact]
        public async Task GetPublicWaitingRoomsAsync_PublicWaitingRoom_IncludesQuiz()
        {
            await SeedAsync(MakeRoom(1, "111111"));

            var room = Assert.Single(await _repository.GetPublicWaitingRoomsAsync());

            Assert.Equal("English Quiz", room.Quiz!.Title);
        }

        // ---- TryCreateRoomAsync ----

        [Fact]
        public async Task TryCreateRoomAsync_NewCode_SavesRoomAndReturnsTrue()
        {
            await SeedAsync();

            var created = await _repository.TryCreateRoomAsync(MakeRoom(0, "123456"));

            Assert.True(created);
            Assert.Equal("123456", (await _db.Rooms.SingleAsync()).JoinCode);
        }

        // The duplicate-code path (Postgres unique violation → false) needs a real Postgres database;
        // RoomService's retry behaviour is covered with a fake repository in RoomServiceTests.

        // ---- AddParticipantAsync ----

        [Fact]
        public async Task AddParticipantAsync_LoadedRoom_AddsParticipantToRoomsCollection()
        {
            await SeedAsync(MakeRoom(1, "123456"));
            var room = (await _repository.GetByJoinCodeAsync("123456"))!;

            var participant = await _repository.AddParticipantAsync(new Participant { RoomId = room.Id, DisplayName = "Anna" });

            Assert.NotEqual(0, participant.Id);
            Assert.Same(participant, Assert.Single(room.Participants));   // EF relationship fix-up
        }

        // ---- GetRoomsByHostIdAsync ----

        [Fact]
        public async Task GetRoomsByHostIdAsync_SeveralHosts_ReturnsOnlyThatHostsRoomsNewestFirst()
        {
            var now = DateTime.UtcNow;
            await SeedAsync(
                MakeRoom(1, "111111", createdAt: now.AddHours(-2)),
                MakeRoom(2, "222222", createdAt: now),
                MakeRoom(3, "333333", hostId: "host-2", createdAt: now.AddHours(-1)));

            var rooms = await _repository.GetRoomsByHostIdAsync("host-1");

            Assert.Equal(new[] { "222222", "111111" }, rooms.Select(r => r.JoinCode));
        }

        // ---- SetStatusAsync ----

        [Fact]
        public async Task SetStatusAsync_StartedAtGiven_UpdatesStatusAndStartedAtOnly()
        {
            await SeedAsync(MakeRoom(1, "111111"));
            var start = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            await _repository.SetStatusAsync(1, RoomStatus.InProgress, startedAt: start);

            var room = await _db.Rooms.SingleAsync();
            Assert.Equal(RoomStatus.InProgress, room.Status);
            Assert.Equal(start, room.StartedAt);
            Assert.Null(room.FinishedAt);
        }

        [Fact]
        public async Task SetStatusAsync_NoTimestampsGiven_KeepsExistingTimestamps()
        {
            var start = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            var room = MakeRoom(1, "111111", status: RoomStatus.InProgress);
            room.StartedAt = start;
            await SeedAsync(room);

            await _repository.SetStatusAsync(1, RoomStatus.Cancelled);

            var saved = await _db.Rooms.SingleAsync();
            Assert.Equal(RoomStatus.Cancelled, saved.Status);
            Assert.Equal(start, saved.StartedAt);
        }

        [Fact]
        public async Task SetStatusAsync_UnknownRoom_DoesNothing()
        {
            await SeedAsync(MakeRoom(1, "111111"));

            await _repository.SetStatusAsync(99, RoomStatus.Finished);

            Assert.Equal(RoomStatus.Waiting, (await _db.Rooms.SingleAsync()).Status);
        }

        // ---- SaveScoresAsync ----

        [Fact]
        public async Task SaveScoresAsync_ScoresForSeveralRooms_WritesOnlyThisRoomsParticipants()
        {
            _db.Participants.AddRange(
                new Participant { Id = 1, RoomId = 1, DisplayName = "Anna" },
                new Participant { Id = 2, RoomId = 2, DisplayName = "Other room" });
            await _db.SaveChangesAsync();

            await _repository.SaveScoresAsync(1, new Dictionary<int, int> { [1] = 150, [2] = 999 });

            Assert.Equal(150, (await _db.Participants.FindAsync(1))!.Score);
            Assert.Equal(0, (await _db.Participants.FindAsync(2))!.Score);
        }

        [Fact]
        public async Task SaveScoresAsync_ParticipantMissingFromScores_KeepsTheirScore()
        {
            _db.Participants.AddRange(
                new Participant { Id = 1, RoomId = 1, DisplayName = "Anna", Score = 0 },
                new Participant { Id = 2, RoomId = 1, DisplayName = "Ben", Score = 40 });
            await _db.SaveChangesAsync();

            await _repository.SaveScoresAsync(1, new Dictionary<int, int> { [1] = 150 });

            Assert.Equal(150, (await _db.Participants.FindAsync(1))!.Score);
            Assert.Equal(40, (await _db.Participants.FindAsync(2))!.Score);
        }

        // ---- RemoveParticipantAsync ----

        [Fact]
        public async Task RemoveParticipantAsync_ExistingParticipant_RemovesOnlyThatParticipant()
        {
            _db.Participants.AddRange(
                new Participant { Id = 1, RoomId = 1, DisplayName = "Anna" },
                new Participant { Id = 2, RoomId = 1, DisplayName = "Ben" });
            await _db.SaveChangesAsync();

            await _repository.RemoveParticipantAsync(1);

            Assert.Equal("Ben", (await _db.Participants.SingleAsync()).DisplayName);
        }

        [Fact]
        public async Task RemoveParticipantAsync_UnknownId_DoesNothing()
        {
            _db.Participants.Add(new Participant { Id = 1, RoomId = 1, DisplayName = "Anna" });
            await _db.SaveChangesAsync();

            await _repository.RemoveParticipantAsync(99);

            Assert.Single(_db.Participants);
        }

        // ---- CancelWaitingRoomsOlderThanAsync ----

        [Fact]
        public async Task CancelWaitingRoomsOlderThanAsync_MixedRooms_CancelsOnlyOldWaitingRoomsAndReturnsCount()
        {
            var now = DateTime.UtcNow;
            await SeedAsync(
                MakeRoom(1, "111111", createdAt: now.AddHours(-2)),
                MakeRoom(2, "222222", createdAt: now),
                MakeRoom(3, "333333", status: RoomStatus.InProgress, createdAt: now.AddHours(-2)));

            var count = await _repository.CancelWaitingRoomsOlderThanAsync(now.AddHours(-1));

            Assert.Equal(1, count);
            Assert.Equal(RoomStatus.Cancelled, (await _db.Rooms.FindAsync(1))!.Status);
            Assert.Equal(RoomStatus.Waiting, (await _db.Rooms.FindAsync(2))!.Status);
            Assert.Equal(RoomStatus.InProgress, (await _db.Rooms.FindAsync(3))!.Status);
        }

        [Fact]
        public async Task CancelWaitingRoomsOlderThanAsync_OldWaitingRoom_SetsFinishedAt()
        {
            var now = DateTime.UtcNow;
            await SeedAsync(MakeRoom(1, "111111", createdAt: now.AddHours(-2)));

            await _repository.CancelWaitingRoomsOlderThanAsync(now.AddHours(-1));

            Assert.NotNull((await _db.Rooms.FindAsync(1))!.FinishedAt);
        }

        // ---- CancelInProgressRoomsAsync ----

        [Fact]
        public async Task CancelInProgressRoomsAsync_MixedRooms_CancelsOnlyInProgressAndReturnsCount()
        {
            await SeedAsync(
                MakeRoom(1, "111111", status: RoomStatus.InProgress),
                MakeRoom(2, "222222", status: RoomStatus.Waiting),
                MakeRoom(3, "333333", status: RoomStatus.Finished));

            var count = await _repository.CancelInProgressRoomsAsync();

            Assert.Equal(1, count);
            Assert.Equal(RoomStatus.Cancelled, (await _db.Rooms.FindAsync(1))!.Status);
            Assert.NotNull((await _db.Rooms.FindAsync(1))!.FinishedAt);
            Assert.Equal(RoomStatus.Waiting, (await _db.Rooms.FindAsync(2))!.Status);
            Assert.Equal(RoomStatus.Finished, (await _db.Rooms.FindAsync(3))!.Status);
        }

        // ---- DeleteRoomAsync ----

        [Fact]
        public async Task DeleteRoomAsync_ExistingRoom_RemovesRoomAndItsParticipants()
        {
            var room = MakeRoom(1, "123456");
            room.Participants = [new Participant { DisplayName = "Anna" }];
            await SeedAsync(room);

            await _repository.DeleteRoomAsync(1);

            Assert.Empty(await _db.Rooms.ToListAsync());
            Assert.Empty(await _db.Participants.ToListAsync());
        }

        [Fact]
        public async Task DeleteRoomAsync_UnknownId_DoesNothing()
        {
            await SeedAsync(MakeRoom(1, "123456"));

            await _repository.DeleteRoomAsync(42);

            Assert.Single(await _db.Rooms.ToListAsync());
        }
    }
}
