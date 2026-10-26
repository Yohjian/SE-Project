using Microsoft.EntityFrameworkCore;
using QuattroLingo.Data;
using QuattroLingo.Entities;
using QuattroLingo.Enums;
using QuattroLingo.Repositories;

namespace QuattroLingo.Tests.Repositories
{
    public class QuizRepositoryTests
    {
        [Theory]
        [InlineData(RoomStatus.Waiting, true)]
        [InlineData(RoomStatus.InProgress, true)]
        [InlineData(RoomStatus.Finished, false)]
        [InlineData(RoomStatus.Cancelled, false)]
        public async Task HasActiveRoom_DependsOnRoomStatus(RoomStatus status, bool expected)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            await using var context = new AppDbContext(options);

            context.Rooms.Add(new Room
            {
                QuizId = 1,
                HostId = "h",
                JoinCode = "111111",
                Status = status
            });

            await context.SaveChangesAsync();

            var repository = new QuizRepository(context);

            Assert.Equal(expected, await repository.HasActiveRoomAsync(1));
            Assert.False(await repository.HasActiveRoomAsync(2));
        }
    }
}
