using Microsoft.EntityFrameworkCore;
using QuattroLingo.Data;
using QuattroLingo.Entities;
using QuattroLingo.Enums;
using QuattroLingo.Interfaces.Repositories;

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
            .Include(r => r.Quiz)
            .Include(r => r.Participants)
            .FirstOrDefaultAsync(r => r.JoinCode == joinCode);
        }

        public async Task<List<Room>> GetPublicWaitingRoomsAsync()
        {
            return await _context.Rooms
            .Include(r => r.Participants)
            .Include(r => r.Quiz)
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

        public async Task<Participant> AddParticipantAsync(Participant participant)
        {
            _context.Participants.Add(participant);
            await _context.SaveChangesAsync();

            return participant;
        }

        public async Task<List<Room>> GetRoomsByHostIdAsync(string hostId)
        {
            return await _context.Rooms
                .Include(r => r.Participants)
                .Include(r => r.Quiz)
                .Where(r => r.HostId == hostId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }
    }
}
