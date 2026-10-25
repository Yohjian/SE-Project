using QuattroLingo.DTOs.Request;
using RoomResponse = QuattroLingo.DTOs.Response.Room;

namespace QuattroLingo.Interfaces.Services
{
    public interface IRoomService
    {
        Task<RoomResponse> CreateRoomAsync(CreateRoom dto, string userId);
        Task<List<RoomResponse>> GetPublicRoomAsync();
        Task<RoomResponse?> GetByCodeAsync(string code);
        Task<RoomResponse> JoinRoomAsync(JoinRoom dto);
        Task<List<RoomResponse>> GetMyRoomsAsync(string userId);
    }
}
