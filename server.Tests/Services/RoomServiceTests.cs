using Microsoft.EntityFrameworkCore;
using NSubstitute;
using QuattroLingo.Data;
using QuattroLingo.DTOs.Request;
using QuattroLingo.Entities;
using QuattroLingo.Enums;
using QuattroLingo.Exceptions;
using QuattroLingo.Interfaces.Repositories;
using QuattroLingo.Repositories;
using QuattroLingo.Services;

namespace QuattroLingo.Tests.Services
{
    public class RoomServiceTests
    {
        private class FakeRoomRepository : IRoomRepository
        {
            public readonly List<Room> Rooms = [];

            public Task<Room?> GetByJoinCodeAsync(string joinCode) =>
                Task.FromResult(Rooms.FirstOrDefault(r => r.JoinCode == joinCode));

            public Task<List<Room>> GetPublicWaitingRoomsAsync() =>
                Task.FromResult(Rooms.Where(r =>
                    r.RoomVisibility == Visibility.Public &&
                    r.Status == RoomStatus.Waiting).ToList());

            public Task<bool> TryCreateRoomAsync(Room room)
            {
                // Mimics the unique index on JoinCode.
                if (Rooms.Any(r => r.JoinCode == room.JoinCode))
                {
                    return Task.FromResult(false);
                }

                room.Id = Rooms.Count + 1;
                Rooms.Add(room);
                return Task.FromResult(true);
            }

            public Task<Participant> AddParticipantAsync(Participant participant)
            {
                // Mimics EF relationship fix-up (see RoomRepositoryTests).
                Rooms.First(r => r.Id == participant.RoomId).Participants.Add(participant);
                return Task.FromResult(participant);
            }

            public Task<List<Room>> GetRoomsByHostIdAsync(string hostId) =>
                Task.FromResult(Rooms.Where(r => r.HostId == hostId).ToList());
        }

        private static IQuizRepository QuizRepoWith(params Quiz[] quizzes)
        {
            var repo = Substitute.For<IQuizRepository>();
            foreach (var quiz in quizzes)
            {
                repo.GetByIdAsync(quiz.Id).Returns(quiz);
            }
            return repo;
        }

        private static Quiz OwnedQuiz(string ownerId = "user-1") =>
            new() { Id = 1, OwnerId = ownerId, Title = "English Quiz" };

        private static Room WaitingRoom(string code = "123456") =>
            new() { Id = 1, QuizId = 1, HostId = "host", JoinCode = code, Status = RoomStatus.Waiting };

        private static (RoomService Service, FakeRoomRepository Repo) ServiceWithRoom(Room room)
        {
            var repo = new FakeRoomRepository();
            repo.Rooms.Add(room);
            return (new RoomService(repo, QuizRepoWith()), repo);
        }

        // ---------- CreateRoom ----------

        [Fact]
        public async Task CreateRoom_OwnQuiz_CreatesPublicWaitingRoom()
        {
            var roomRepo = new FakeRoomRepository();
            var service = new RoomService(roomRepo, QuizRepoWith(OwnedQuiz()));

            var result = await service.CreateRoomAsync(
                new CreateRoom { QuizId = 1, RoomVisibility = Visibility.Public }, "user-1");

            var room = Assert.Single(roomRepo.Rooms);
            Assert.Equal(1, room.QuizId);
            Assert.Equal("user-1", room.HostId);
            Assert.Equal(Visibility.Public, room.RoomVisibility);
            Assert.Equal(RoomStatus.Waiting, room.Status);

            Assert.Equal("English Quiz", result.QuizTitle);
            Assert.Equal(6, result.JoinCode.Length);
            Assert.Equal(0, result.ParticipantCount);
        }

        [Fact]
        public async Task CreateRoom_Twice_GeneratesDifferentJoinCodes()
        {
            var roomRepo = new FakeRoomRepository();
            var service = new RoomService(roomRepo, QuizRepoWith(OwnedQuiz()));
            var request = new CreateRoom { QuizId = 1, RoomVisibility = Visibility.Public };

            var first = await service.CreateRoomAsync(request, "user-1");
            var second = await service.CreateRoomAsync(request, "user-1");

            Assert.NotEqual(first.JoinCode, second.JoinCode);
            Assert.Equal(2, roomRepo.Rooms.Count);
        }

        [Fact]
        public async Task CreateRoom_CodeCollision_RetriesWithNewCode()
        {
            var roomRepo = Substitute.For<IRoomRepository>();
            roomRepo.TryCreateRoomAsync(Arg.Any<Room>()).Returns(false, false, true);
            var service = new RoomService(roomRepo, QuizRepoWith(OwnedQuiz()));

            await service.CreateRoomAsync(
                new CreateRoom { QuizId = 1, RoomVisibility = Visibility.Public }, "user-1");

            await roomRepo.Received(3).TryCreateRoomAsync(Arg.Any<Room>());
        }

        [Fact]
        public async Task CreateRoom_EveryCodeCollides_GivesUp()
        {
            var roomRepo = Substitute.For<IRoomRepository>();
            roomRepo.TryCreateRoomAsync(Arg.Any<Room>()).Returns(false);
            var service = new RoomService(roomRepo, QuizRepoWith(OwnedQuiz()));

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateRoomAsync(
                new CreateRoom { QuizId = 1, RoomVisibility = Visibility.Public }, "user-1"));
        }

        [Fact]
        public async Task CreateRoom_QuizOwnedByAnotherUser_ThrowsForbidden()
        {
            var roomRepo = new FakeRoomRepository();
            var service = new RoomService(roomRepo, QuizRepoWith(OwnedQuiz("user-1")));

            await Assert.ThrowsAsync<ForbiddenException>(() => service.CreateRoomAsync(
                new CreateRoom { QuizId = 1, RoomVisibility = Visibility.Public }, "user-2"));

            Assert.Empty(roomRepo.Rooms);
        }

        [Fact]
        public async Task CreateRoom_UnknownQuiz_ThrowsNotFound()
        {
            var service = new RoomService(new FakeRoomRepository(), QuizRepoWith());

            await Assert.ThrowsAsync<NotFoundException>(() => service.CreateRoomAsync(
                new CreateRoom { QuizId = 99, RoomVisibility = Visibility.Public }, "user-1"));
        }

        [Fact]
        public async Task CreateRoom_InvalidVisibility_ThrowsValidation()
        {
            var service = new RoomService(new FakeRoomRepository(), QuizRepoWith(OwnedQuiz()));

            await Assert.ThrowsAsync<ValidationException>(() => service.CreateRoomAsync(
                new CreateRoom { QuizId = 1, RoomVisibility = (Visibility)99 }, "user-1"));
        }

        // ---------- Lookup ----------

        [Fact]
        public async Task GetPublicRooms_ExcludesPrivateRooms()
        {
            var (service, repo) = ServiceWithRoom(WaitingRoom("123456"));
            repo.Rooms.Add(new Room
            {
                Id = 2,
                QuizId = 1,
                HostId = "host",
                JoinCode = "654321",
                RoomVisibility = Visibility.Private,
                Status = RoomStatus.Waiting
            });

            var result = await service.GetPublicRoomAsync();

            var room = Assert.Single(result);
            Assert.Equal("123456", room.JoinCode);
            Assert.Equal(Visibility.Public, room.Visibility);
        }

        [Fact]
        public async Task GetByCode_TrimsWhitespace()
        {
            var (service, _) = ServiceWithRoom(WaitingRoom("123456"));

            var result = await service.GetByCodeAsync(" 123456 ");

            Assert.Equal("123456", result.JoinCode);
        }

        [Theory]
        [InlineData("999999")]   // well-formed but unknown
        [InlineData("abc")]      // malformed
        [InlineData("")]
        public async Task GetByCode_UnknownOrMalformed_ThrowsNotFound(string code)
        {
            var (service, _) = ServiceWithRoom(WaitingRoom("123456"));

            await Assert.ThrowsAsync<NotFoundException>(() => service.GetByCodeAsync(code));
        }

        // ---------- Join ----------

        [Fact]
        public async Task JoinRoom_ValidRequest_AddsTrimmedParticipantAndCountsCorrectly()
        {
            var (service, repo) = ServiceWithRoom(WaitingRoom());

            var first = await service.JoinRoomAsync(
                new JoinRoom { JoinCode = "123456", DisplayName = "  Anna  " });
            var second = await service.JoinRoomAsync(
                new JoinRoom { JoinCode = "123456", DisplayName = "Bob" });

            Assert.Equal(1, first.ParticipantCount);
            Assert.Equal(2, second.ParticipantCount);
            Assert.Equal("Anna", repo.Rooms[0].Participants.First().DisplayName);
        }

        [Fact]
        public async Task JoinRoom_WithRealRepository_ReportsCorrectParticipantCount()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            await using var context = new AppDbContext(options);
            context.Quizzes.Add(new Quiz { Id = 1, OwnerId = "user-1", Title = "English Quiz" });
            context.Rooms.Add(new Room { QuizId = 1, HostId = "host", JoinCode = "123456" });
            await context.SaveChangesAsync();

            var service = new RoomService(new RoomRepository(context), QuizRepoWith());

            var first = await service.JoinRoomAsync(
                new JoinRoom { JoinCode = "123456", DisplayName = "Anna" });
            var second = await service.JoinRoomAsync(
                new JoinRoom { JoinCode = "123456", DisplayName = "Bob" });

            Assert.Equal(1, first.ParticipantCount);
            Assert.Equal(2, second.ParticipantCount);
        }

        [Fact]
        public async Task JoinRoom_UnknownRoom_ThrowsNotFound()
        {
            var service = new RoomService(new FakeRoomRepository(), QuizRepoWith());

            await Assert.ThrowsAsync<NotFoundException>(() => service.JoinRoomAsync(
                new JoinRoom { JoinCode = "123456", DisplayName = "Anna" }));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task JoinRoom_BlankName_ThrowsValidation(string name)
        {
            var (service, _) = ServiceWithRoom(WaitingRoom());

            await Assert.ThrowsAsync<ValidationException>(() => service.JoinRoomAsync(
                new JoinRoom { JoinCode = "123456", DisplayName = name }));
        }

        [Fact]
        public async Task JoinRoom_NameTooLong_ThrowsValidation()
        {
            var (service, _) = ServiceWithRoom(WaitingRoom());
            var name = new string('x', RoomService.MaxDisplayNameLength + 1);

            await Assert.ThrowsAsync<ValidationException>(() => service.JoinRoomAsync(
                new JoinRoom { JoinCode = "123456", DisplayName = name }));
        }

        [Fact]
        public async Task JoinRoom_DuplicateNameIgnoringCase_ThrowsConflict()
        {
            var room = WaitingRoom();
            room.Participants.Add(new Participant { RoomId = 1, DisplayName = "Anna" });
            var (service, _) = ServiceWithRoom(room);

            await Assert.ThrowsAsync<ConflictException>(() => service.JoinRoomAsync(
                new JoinRoom { JoinCode = "123456", DisplayName = " aNNa " }));
        }

        [Theory]
        [InlineData(RoomStatus.InProgress)]
        [InlineData(RoomStatus.Finished)]
        [InlineData(RoomStatus.Cancelled)]
        public async Task JoinRoom_RoomNotWaiting_ThrowsConflict(RoomStatus status)
        {
            var room = WaitingRoom();
            room.Status = status;
            var (service, _) = ServiceWithRoom(room);

            await Assert.ThrowsAsync<ConflictException>(() => service.JoinRoomAsync(
                new JoinRoom { JoinCode = "123456", DisplayName = "Anna" }));
        }

        [Fact]
        public async Task JoinRoom_RoomFull_ThrowsConflict()
        {
            var room = WaitingRoom();
            for (var i = 0; i < RoomService.MaxParticipants; i++)
            {
                room.Participants.Add(new Participant { RoomId = 1, DisplayName = $"Player{i}" });
            }
            var (service, _) = ServiceWithRoom(room);

            await Assert.ThrowsAsync<ConflictException>(() => service.JoinRoomAsync(
                new JoinRoom { JoinCode = "123456", DisplayName = "OneTooMany" }));
        }
    }
}