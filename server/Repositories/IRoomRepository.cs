using QuattroLingo.Entities;

namespace QuattroLingo.Repositories
{
    public interface IRoomRepository
    {
        Task<Room?> GetByJoinCodeAsync(string joinCode);
        Task<List<Room>> GetPublicWaitingRoomsAsync();
        Task<Room> CreateRoomAsync(Room room);
        Task<Participant> AddParticipantAsync(Participant participant);
        Task<List<Room>> GetRoomsByHostIdAsync(string hostId);
    }
}
