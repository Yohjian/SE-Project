namespace QuattroLingo.DTOs.Response
{
    public class JoinedRoom
    {
        public Room Room { get; set; } = null!;
        public int ParticipantId { get; set; }
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>Secret proving "I am this participant". Returned once, never stored in plain text.</summary>
        public string PlayerToken { get; set; } = string.Empty;
    }
}