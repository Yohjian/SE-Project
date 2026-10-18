using QuattroLingo.Enums;

namespace QuattroLingo.DTOs.Request
{
    public class CreateRoom
    {
        public int QuizId { get; set; }
        public Visibility RoomVisibility { get; set;}
    }
}