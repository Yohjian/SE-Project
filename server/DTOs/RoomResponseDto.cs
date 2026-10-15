using QuattroLingo.Enums;

namespace QuattroLingo.DTOs
{
    public class RoomResponseDto
    {
        public int Id { get; set; }
        public string QuizTitle { get; set; } = string.Empty;
        public int QuizId { get; set; }
        public string JoinCode { get; set; } = string.Empty;
        public Visibility Visibility { get; set; }
        public RoomStatus Status { get; set; }
        public int ParticipantCount { get; set; }
    }
}