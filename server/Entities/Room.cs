using QuattroLingo.Enums;
namespace QuattroLingo.Entities
{
    public class Room
    {
        public int Id { get; set; }
        public int QuizId { get; set; }
        public string HostId { get; set; } = string.Empty;
        public string JoinCode { get; set; } = string.Empty;
        public Visibility RoomVisibility { get; set; } = Visibility.Public;
        public RoomStatus Status { get; set; } = RoomStatus.Waiting;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? StartedAt { get; set; }
        public DateTime? FinishedAt {get; set;}
    }
}