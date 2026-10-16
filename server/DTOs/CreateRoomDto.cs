using QuattroLingo.Enums;

namespace QuattroLingo.DTOs
{
    public class CreateRoomDto
    {
        public int QuizId { get; set; }
        public Visibility RoomVisibility { get; set;}
    }
}