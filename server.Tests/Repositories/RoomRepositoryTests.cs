using Microsoft.EntityFrameworkCore;
using QuattroLingo.Data;
using QuattroLingo.Entities;
using QuattroLingo.Repositories;

namespace QuattroLingo.Tests.Repositories
{
    public class RoomRepositoryTests
    {
        [Fact]
        public async Task AddParticipant_ToLoadedRoom_AppearsInRoomParticipants()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            await using var context = new AppDbContext(options);

            context.Quizzes.Add(new Quiz
            {
                Id = 1,
                OwnerId = "user-1",
                Title = "English Quiz"
            });

            context.Rooms.Add(new Room
            {
                QuizId = 1,
                HostId = "host",
                JoinCode = "123456"
            });

            await context.SaveChangesAsync();

            var repo = new RoomRepository(context);
            var room = await repo.GetByJoinCodeAsync("123456");

            await repo.AddParticipantAsync(new Participant
            {
                RoomId = room!.Id,
                DisplayName = "Anna"
            });

            // EF relationship fix-up: the loaded room's collection is updated.
            Assert.Single(room.Participants);
        }
    }
}
