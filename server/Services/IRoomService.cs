using QuattroLingo.DTOs;

namespace QuattroLingoServices
{
    public interface IroomServices
    {
        Task<RoomResponseDto> CreateRoomAsync(CreateRoomDto dto, string userId);
        Task<List<RoomResponseDto>> GetPublicRoomAsync();
        Task<RoomResponseDto?> GetByCodeAsync(string code);
        Task<RoomResponseDto> JoinRoomAsync(JoinRoomDto dto);
    }
}