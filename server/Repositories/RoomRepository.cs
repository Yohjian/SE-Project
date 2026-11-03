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

        public async Task SetStatusAsync(int roomId, RoomStatus status, DateTime? startedAt = null, DateTime? finishedAt = null)
        {
            var room = await _context.Rooms.FirstOrDefaultAsync(r => r.Id == roomId);
            if (room is null) return;

            room.Status = status;
            if (startedAt is not null) room.StartedAt = startedAt;
            if (finishedAt is not null) room.FinishedAt = finishedAt;
            await _context.SaveChangesAsync();
        }

        public async Task SaveScoresAsync(int roomId, IReadOnlyDictionary<int, int> scoresByParticipantId)
        {
            var participants = await _context.Participants.Where(p => p.RoomId == roomId).ToListAsync();
            foreach (var participant in participants)
            {
                if (scoresByParticipantId.TryGetValue(participant.Id, out var score))
                {
                    participant.Score = score;
                }
            }
            await _context.SaveChangesAsync();
        }

        public async Task RemoveParticipantAsync(int participantId)
        {
            var participant = await _context.Participants.FirstOrDefaultAsync(p => p.Id == participantId);
            if (participant is null) return;

            _context.Participants.Remove(participant);
            await _context.SaveChangesAsync();
        }

        public async Task<int> CancelWaitingRoomsOlderThanAsync(DateTime cutoffUtc)
        {
            var rooms = await _context.Rooms
                .Where(r => r.Status == RoomStatus.Waiting && r.CreatedAt < cutoffUtc)
                .ToListAsync();

            foreach (var room in rooms)
            {
                room.Status = RoomStatus.Cancelled;
                room.FinishedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
            return rooms.Count;
        }

        public async Task<int> CancelInProgressRoomsAsync()
        {
            var rooms = await _context.Rooms.Where(r => r.Status == RoomStatus.InProgress).ToListAsync();

            foreach (var room in rooms)
            {
                room.Status = RoomStatus.Cancelled;
                room.FinishedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
            return rooms.Count;
        }

    }
}
