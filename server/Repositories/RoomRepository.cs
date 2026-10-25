using Microsoft.EntityFrameworkCore;
using QuattroLingo.Data;
using QuattroLingo.Entities;
using QuattroLingo.Enums;
using QuattroLingo.Interfaces.Repositories;
using Npgsql;

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

        public async Task<bool> TryCreateRoomAsync(Room room)
        {
            _context.Rooms.Add(room);

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException ex)
                when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                // Stop the context from retrying the failed insert on the next SaveChanges.
                _context.Entry(room).State = EntityState.Detached;
                return false;
            }
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
