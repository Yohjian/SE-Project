using QuattroLingo.Entities;
using QuattroLingo.Enums;

namespace QuattroLingo.Interfaces.Repositories
{
    public interface IRoomRepository
    {
        Task<Room?> GetByJoinCodeAsync(string joinCode);
        Task<List<Room>> GetPublicWaitingRoomsAsync();
        Task<bool> TryCreateRoomAsync(Room room);
        Task<Participant> AddParticipantAsync(Participant participant);
        Task<List<Room>> GetRoomsByHostIdAsync(string hostId);
        Task SetStatusAsync(int roomId, RoomStatus status, DateTime? startedAt = null, DateTime? finishedAt = null);
        Task SaveScoresAsync(int roomId, IReadOnlyDictionary<int, int> scoresByParticipantId);
        Task RemoveParticipantAsync(int participantId);
        Task<int> CancelWaitingRoomsOlderThanAsync(DateTime cutoffUtc);
        Task<int> CancelInProgressRoomsAsync();
    }
}