using QuattroLingo.Entities;

namespace QuattroLingo.Interfaces.Repositories
{
    public interface IRoomRepository
    {
        Task<Room?> GetByJoinCodeAsync(string joinCode);
        Task<List<Room>> GetPublicWaitingRoomsAsync();
        Task<bool> TryCreateRoomAsync(Room room);
        Task<Participant> AddParticipantAsync(Participant participant);
        Task<List<Room>> GetRoomsByHostIdAsync(string hostId);
    }
}