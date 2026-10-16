using QuattroLingo.DTOs;

namespace QuattroLingo.Services
{
    public interface IRoomService
    {
        Task<RoomResponseDto> CreateRoomAsync(CreateRoomDto dto, string userId);
        Task<List<RoomResponseDto>> GetPublicRoomAsync();
        Task<RoomResponseDto?> GetByCodeAsync(string code);
        Task<RoomResponseDto> JoinRoomAsync(JoinRoomDto dto);
        Task<List<RoomResponseDto>> GetMyRoomsAsync(string userId);
    }
}