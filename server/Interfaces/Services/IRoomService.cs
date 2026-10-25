using QuattroLingo.DTOs.Request;
using QuattroLingo.DTOs.Response;
using RoomResponse = QuattroLingo.DTOs.Response.Room;

namespace QuattroLingo.Interfaces.Services
{
    public interface IRoomService
    {
        Task<RoomResponse> CreateRoomAsync(CreateRoom dto, string userId);
        Task<List<RoomResponse>> GetPublicRoomAsync();
        Task<RoomResponse> GetByCodeAsync(string code);
        Task<JoinedRoom> JoinRoomAsync(JoinRoom dto);
        Task<List<RoomResponse>> GetMyRoomsAsync(string userId);
    }
}
