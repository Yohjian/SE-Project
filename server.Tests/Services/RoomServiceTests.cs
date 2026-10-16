using QuattroLingo.DTOs;
using QuattroLingo.Entities;
using QuattroLingo.Enums;
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
                Task.FromResult(
                    Rooms.FirstOrDefault(r => r.JoinCode == joinCode)
                );

            public Task<List<Room>> GetPublicWaitingRoomsAsync() =>
                Task.FromResult(
                    Rooms.Where(r =>
                        r.RoomVisibility == Visibility.Public &&
                        r.Status == RoomStatus.Waiting
                    ).ToList()
                );

            public Task<Room> CreateRoomAsync(Room room)
            {
                room.Id = Rooms.Count + 1;
                Rooms.Add(room);
                return Task.FromResult(room);
            }

            public Task<Participant> AddParticipantAsync(Participant participant) =>
                Task.FromResult(participant);

            public Task<List<Room>> GetRoomsByHostIdAsync(string hostId) =>
                Task.FromResult(
                    Rooms.Where(r => r.HostId == hostId).ToList()
                );
        }
        private class FakeQuizRepository : IQuizRepository
        {
            public readonly List<Quiz> Quizzes = [];

            public Task<Quiz?> GetByIdAsync(int id) =>
                Task.FromResult(Quizzes.FirstOrDefault(q => q.Id == id));

            public Task<List<Quiz>> GetByOwnerIdAsync(string ownerId) =>
                Task.FromResult(Quizzes.Where(q => q.OwnerId == ownerId).ToList());

            public Task AddAsync(Quiz quiz)
            {
                Quizzes.Add(quiz);
                return Task.CompletedTask;
            }

            public Task UpdateAsync(Quiz quiz) => Task.CompletedTask;
            public Task DeleteAsync(Quiz quiz) => Task.CompletedTask;

            public Task<Question?> GetQuestionByIdAsync(int id) =>
                Task.FromResult<Question?>(null);

            public Task<AnswerOption?> GetAnswerOptionByIdAsync(int id) =>
                Task.FromResult<AnswerOption?>(null);

            public Task AddQuestionAsync(Question question) => Task.CompletedTask;
            public Task DeleteQuestionAsync(Question question) => Task.CompletedTask;
            public Task AddAnswerOptionAsync(AnswerOption option) => Task.CompletedTask;
            public Task DeleteAnswerOptionAsync(AnswerOption option) => Task.CompletedTask;
            public Task SaveChangesAsync() => Task.CompletedTask;
        }

        [Fact]
        public async Task CreateRoom_OwnQuiz_CreatesPublicWaitingRoom()
        {
            // Arrange
            var roomRepo = new FakeRoomRepository();
            var quizRepo = new FakeQuizRepository();

            quizRepo.Quizzes.Add(new Quiz
            {
                Id = 1,
                OwnerId = "user-1",
                Title = "English Quiz"
            });

            var service = new RoomService(roomRepo, quizRepo);

            var request = new CreateRoomDto
            {
                QuizId = 1,
                RoomVisibility = Visibility.Public
            };

            // Act
            var result = await service.CreateRoomAsync(request, "user-1");

            // Assert
            var room = Assert.Single(roomRepo.Rooms);

            Assert.Equal(1, room.QuizId);
            Assert.Equal("user-1", room.HostId);
            Assert.Equal(Visibility.Public, room.RoomVisibility);
            Assert.Equal(RoomStatus.Waiting, room.Status);

            Assert.Equal("English Quiz", result.QuizTitle);
            Assert.Equal(6, result.JoinCode.Length);
        }

        [Fact]
        public async Task CreateRoom_Twice_GeneratesDifferentJoinCodes()
        {
            // Arrange
            var roomRepo = new FakeRoomRepository();
            var quizRepo = new FakeQuizRepository();

            quizRepo.Quizzes.Add(new Quiz
            {
                Id = 1,
                OwnerId = "user-1",
                Title = "English Quiz"
            });

            var service = new RoomService(roomRepo, quizRepo);

            var request = new CreateRoomDto
            {
                QuizId = 1,
                RoomVisibility = Visibility.Public
            };

            // Act
            var first = await service.CreateRoomAsync(request, "user-1");
            var second = await service.CreateRoomAsync(request, "user-1");

            // Assert
            Assert.NotEqual(first.JoinCode, second.JoinCode);
            Assert.Equal(2, roomRepo.Rooms.Count);
        }

        [Fact]
        public async Task GetPublicRooms_ExcludesPrivateRooms()
        {
            // Arrange
            var roomRepo = new FakeRoomRepository();
            var quizRepo = new FakeQuizRepository();
            var service = new RoomService(roomRepo, quizRepo);

            roomRepo.Rooms.Add(new Room
            {
                Id = 1,
                QuizId = 1,
                HostId = "user-1",
                JoinCode = "123456",
                RoomVisibility = Visibility.Public,
                Status = RoomStatus.Waiting
            });

            roomRepo.Rooms.Add(new Room
            {
                Id = 2,
                QuizId = 1,
                HostId = "user-1",
                JoinCode = "654321",
                RoomVisibility = Visibility.Private,
                Status = RoomStatus.Waiting
            });

            // Act
            var result = await service.GetPublicRoomAsync();

            // Assert
            var room = Assert.Single(result);

            Assert.Equal("123456", room.JoinCode);
            Assert.Equal(Visibility.Public, room.Visibility);
        }

        [Fact]
        public async Task CreateRoom_QuizOwnedByAnotherUser_ThrowsUnauthorized()
        {
            // Arrange
            var roomRepo = new FakeRoomRepository();
            var quizRepo = new FakeQuizRepository();

            quizRepo.Quizzes.Add(new Quiz
            {
                Id = 1,
                OwnerId = "user-1",
                Title = "English Quiz"
            });

            var service = new RoomService(roomRepo, quizRepo);

            var request = new CreateRoomDto
            {
                QuizId = 1,
                RoomVisibility = Visibility.Public
            };

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => service.CreateRoomAsync(request, "user-2")
            );

            Assert.Empty(roomRepo.Rooms);
        }

    }
}
