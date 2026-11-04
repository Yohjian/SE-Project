using QuattroLingo.Entities;
using RoomResponse = QuattroLingo.DTOs.Response.Room;

namespace QuattroLingo.Extensions
{
    public static class RoomMappingExtensions
    {
        public static RoomResponse ToResponse(this Room room, int? participantCount = null) => new()
        {
            Id = room.Id,
            QuizId = room.QuizId,
            QuizTitle = room.Quiz?.Title ?? "Unknown Quiz",
            JoinCode = room.JoinCode,
            Visibility = room.RoomVisibility,
            Status = room.Status,
            ParticipantCount = participantCount ?? room.Participants.Count
        };
    }
}