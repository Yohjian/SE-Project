using Microsoft.EntityFrameworkCore;
using QuattroLingo.Data;
using QuattroLingo.Entities;
using QuattroLingo.Enums;

namespace QuattroLingo.Repositories
{
    public class RoomRepository : IRoomRepository
    {
        private readonly AppDbContext _context;

        public RoomRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Room?> GetByJoinCodeAsync(string joinCode)
        {
            return await _context.Rooms
                .FirstOrDefaultAsync(r => r.JoinCode == joinCode);
        }

        public async Task<List<Room>> GetPublicWaitingRoomsAsync()
        {
            return await _context.Rooms
                .Where(r => r.RoomVisibility == Visibility.Public)
                .Where(r => r.Status == RoomStatus.Waiting)
                .ToListAsync();
        }

        public async Task<Room> CreateRoomAsync(Room room)
        {
            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();
            return room;
        }
    }
}