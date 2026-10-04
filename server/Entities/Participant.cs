namespace QuattroLingo.Entities
{
    public class Participant
    {
        public int Id { get; set; }
        public int RoomId { get; set; }
        public string? UserId { get; set; }
        public string DisplayName { get; set;} = string.Empty;
        public int Score { get; set; } = 0;
    }
}